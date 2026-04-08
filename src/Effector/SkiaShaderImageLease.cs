using System;
using SkiaSharp;

namespace Effector;

public sealed class SkiaShaderImageLease : IDisposable
{
    private readonly long _handleId;
    private bool _isDisposed;

    internal SkiaShaderImageLease(long handleId, SKImage image)
    {
        _handleId = handleId;
        Image = image ?? throw new ArgumentNullException(nameof(image));
    }

    public SKImage Image { get; }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        SkiaShaderImageRegistry.ReleaseLease(_handleId);
        GC.SuppressFinalize(this);
    }
}
