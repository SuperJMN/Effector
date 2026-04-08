using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace Effector;

public static class SkiaShaderImageRegistry
{
    private sealed class ImageEntry : IDisposable
    {
        public ImageEntry(SKImage image, SKBitmap? bitmap)
        {
            Image = image;
            Bitmap = bitmap;
        }

        private int _leaseCount;
        private int _released;

        public SKImage Image { get; }

        private SKBitmap? Bitmap { get; }

        public bool IsReleased => Volatile.Read(ref _released) != 0;

        public bool TryAcquireLease(out bool removeReleasedEntry)
        {
            removeReleasedEntry = false;
            if (IsReleased)
            {
                return false;
            }

            Interlocked.Increment(ref _leaseCount);
            if (!IsReleased)
            {
                return true;
            }

            removeReleasedEntry = ReleaseLease();
            return false;
        }

        public bool ReleaseHandle()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0)
            {
                return false;
            }

            return Volatile.Read(ref _leaseCount) == 0;
        }

        public bool ReleaseLease()
        {
            var remaining = Interlocked.Decrement(ref _leaseCount);
            return remaining == 0 && IsReleased;
        }

        public void Dispose()
        {
            Image.Dispose();
            Bitmap?.Dispose();
        }
    }

    private static readonly ConcurrentDictionary<long, ImageEntry> Images = new();
    private static long s_nextHandleId;

    public static SkiaShaderImageHandle Register(Bitmap bitmap)
    {
        if (bitmap is null)
        {
            throw new ArgumentNullException(nameof(bitmap));
        }

        var image = CreateImage(bitmap, out var backingBitmap);

        var id = Interlocked.Increment(ref s_nextHandleId);
        if (!Images.TryAdd(id, new ImageEntry(image, backingBitmap)))
        {
            image.Dispose();
            backingBitmap?.Dispose();
            throw new InvalidOperationException("The shader image handle could not be registered.");
        }

        return new SkiaShaderImageHandle(id);
    }

    public static bool TryGetImage(SkiaShaderImageHandle handle, out SKImage? image)
    {
        if (handle.IsEmpty || !Images.TryGetValue(handle.Value, out var entry) || entry.IsReleased)
        {
            image = null;
            return false;
        }

        image = entry.Image;
        return true;
    }

    public static bool TryAcquire(SkiaShaderImageHandle handle, out SkiaShaderImageLease? lease)
    {
        lease = null;
        if (handle.IsEmpty || !Images.TryGetValue(handle.Value, out var entry))
        {
            return false;
        }

        if (!entry.TryAcquireLease(out var removeReleasedEntry))
        {
            DisposeReleasedEntry(handle.Value, removeReleasedEntry);
            return false;
        }

        lease = new SkiaShaderImageLease(handle.Value, entry.Image);
        return true;
    }

    public static void Release(SkiaShaderImageHandle handle)
    {
        if (handle.IsEmpty)
        {
            return;
        }

        if (!Images.TryGetValue(handle.Value, out var entry))
        {
            return;
        }

        DisposeReleasedEntry(handle.Value, entry.ReleaseHandle());
    }

    internal static void ReleaseLease(long handleId)
    {
        if (!Images.TryGetValue(handleId, out var entry))
        {
            return;
        }

        DisposeReleasedEntry(handleId, entry.ReleaseLease());
    }

    private static void DisposeReleasedEntry(long handleId, bool shouldRemove)
    {
        if (!shouldRemove)
        {
            return;
        }

        if (Images.TryRemove(handleId, out var entry))
        {
            entry.Dispose();
        }
    }

    private static SKImage CreateImage(Bitmap bitmap, out SKBitmap? backingBitmap)
    {
        if (TryCreateDirectImage(bitmap, out var image, out backingBitmap))
        {
            return image;
        }

        backingBitmap = null;
        return CreateEncodedImage(bitmap);
    }

    private static bool TryCreateDirectImage(Bitmap bitmap, out SKImage image, out SKBitmap? backingBitmap)
    {
        image = null!;
        backingBitmap = null;

        var pixelSize = bitmap.PixelSize;
        if (pixelSize.Width <= 0 || pixelSize.Height <= 0)
        {
            return false;
        }

        var colorType = bitmap.Format == PixelFormat.Rgba8888
            ? SKColorType.Rgba8888
            : bitmap.Format == PixelFormat.Bgra8888
                ? SKColorType.Bgra8888
                : SKColorType.Bgra8888;
        var alphaType = bitmap.AlphaFormat == AlphaFormat.Opaque
            ? SKAlphaType.Opaque
            : bitmap.AlphaFormat == AlphaFormat.Unpremul
                ? SKAlphaType.Unpremul
                : SKAlphaType.Premul;
        var imageInfo = new SKImageInfo(pixelSize.Width, pixelSize.Height, colorType, alphaType);
        backingBitmap = new SKBitmap(imageInfo);
        var pixels = backingBitmap.GetPixels();
        if (pixels == IntPtr.Zero)
        {
            backingBitmap.Dispose();
            backingBitmap = null;
            return false;
        }

        try
        {
            bitmap.CopyPixels(
                new PixelRect(0, 0, pixelSize.Width, pixelSize.Height),
                pixels,
                imageInfo.BytesSize,
                backingBitmap.RowBytes);
        }
        catch
        {
            backingBitmap.Dispose();
            backingBitmap = null;
            return false;
        }

        image = SKImage.FromBitmap(backingBitmap);
        if (image is not null)
        {
            return true;
        }

        backingBitmap.Dispose();
        backingBitmap = null;
        return false;
    }

    private static SKImage CreateEncodedImage(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        using var data = SKData.CreateCopy(stream.ToArray());
        var image = SKImage.FromEncodedData(data);
        if (image is null)
        {
            throw new InvalidOperationException("The bitmap could not be converted to an SKImage.");
        }

        return image;
    }
}
