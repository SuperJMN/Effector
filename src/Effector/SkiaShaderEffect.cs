using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Effector;

public sealed class SkiaShaderEffect : IDisposable
{
    private readonly IDisposable[] _ownedResources;

    public SkiaShaderEffect(
        SKShader? shader,
        SKBlendMode blendMode = SKBlendMode.SrcOver,
        bool isAntialias = true,
        SKRect? destinationRect = null,
        SKMatrix? localMatrix = null,
        Action<SKCanvas, SKImage, SKRect>? fallbackRenderer = null,
        IEnumerable<IDisposable>? ownedResources = null,
        bool maskToContent = true)
    {
        if (shader is null && fallbackRenderer is null)
        {
            throw new ArgumentException("A shader effect requires either a shader or a fallback renderer.", nameof(shader));
        }

        Shader = shader;
        BlendMode = blendMode;
        IsAntialias = isAntialias;
        DestinationRect = destinationRect;
        LocalMatrix = localMatrix;
        FallbackRenderer = fallbackRenderer;
        MaskToContent = maskToContent;
        _ownedResources = ownedResources is null
            ? Array.Empty<IDisposable>()
            : new List<IDisposable>(ownedResources).ToArray();
    }

    public SKShader? Shader { get; }

    public SKBlendMode BlendMode { get; }

    public bool IsAntialias { get; }

    public SKRect? DestinationRect { get; }

    public SKMatrix? LocalMatrix { get; }

    public Action<SKCanvas, SKImage, SKRect>? FallbackRenderer { get; }

    /// <summary>
    /// When true (default), the runtime masks the shader output by the captured
    /// visual's alpha (DstIn), so output is clipped to the visible silhouette.
    /// When false, the shader fills its requested bounds even where the source
    /// visual is transparent — required for auras, glares, mists, halos and
    /// other "outside the silhouette" effects.
    /// </summary>
    public bool MaskToContent { get; }

    public void RenderFallback(SKCanvas canvas, SKImage contentImage)
    {
        if (canvas is null)
        {
            throw new ArgumentNullException(nameof(canvas));
        }

        if (contentImage is null)
        {
            throw new ArgumentNullException(nameof(contentImage));
        }

        FallbackRenderer?.Invoke(canvas, contentImage, DestinationRect ?? SKRect.Create(contentImage.Width, contentImage.Height));
    }

    public void Dispose()
    {
        Shader?.Dispose();

        for (var index = _ownedResources.Length - 1; index >= 0; index--)
        {
            _ownedResources[index].Dispose();
        }
    }
}
