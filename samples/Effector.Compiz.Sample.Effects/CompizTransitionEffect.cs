using System;
using Avalonia;
using Effector;
using SkiaSharp;

namespace Effector.Compiz.Sample.Effects;

[SkiaEffect(typeof(CompizTransitionEffectFactory))]
public sealed class CompizTransitionEffect : SkiaEffectBase
{
    public static readonly StyledProperty<CompizTransitionKind> KindProperty =
        AvaloniaProperty.Register<CompizTransitionEffect, CompizTransitionKind>(nameof(Kind), CompizTransitionKind.Dissolve);

    public static readonly StyledProperty<SkiaShaderImageHandle> FromImageProperty =
        AvaloniaProperty.Register<CompizTransitionEffect, SkiaShaderImageHandle>(nameof(FromImage));

    public static readonly StyledProperty<SkiaShaderImageHandle> ToImageProperty =
        AvaloniaProperty.Register<CompizTransitionEffect, SkiaShaderImageHandle>(nameof(ToImage));

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<CompizTransitionEffect, double>(nameof(Progress), 0d);

    public static readonly StyledProperty<double> TimeProperty =
        AvaloniaProperty.Register<CompizTransitionEffect, double>(nameof(Time), 0d);

    public static readonly StyledProperty<double> ClickXProperty =
        AvaloniaProperty.Register<CompizTransitionEffect, double>(nameof(ClickX), 0.5d);

    public static readonly StyledProperty<double> ClickYProperty =
        AvaloniaProperty.Register<CompizTransitionEffect, double>(nameof(ClickY), 0.5d);

    static CompizTransitionEffect()
    {
        AffectsRender<CompizTransitionEffect>(
            KindProperty,
            FromImageProperty,
            ToImageProperty,
            ProgressProperty,
            TimeProperty,
            ClickXProperty,
            ClickYProperty);
    }

    public CompizTransitionKind Kind
    {
        get => GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public SkiaShaderImageHandle FromImage
    {
        get => GetValue(FromImageProperty);
        set => SetValue(FromImageProperty, value);
    }

    public SkiaShaderImageHandle ToImage
    {
        get => GetValue(ToImageProperty);
        set => SetValue(ToImageProperty, value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public double Time
    {
        get => GetValue(TimeProperty);
        set => SetValue(TimeProperty, value);
    }

    public double ClickX
    {
        get => GetValue(ClickXProperty);
        set => SetValue(ClickXProperty, value);
    }

    public double ClickY
    {
        get => GetValue(ClickYProperty);
        set => SetValue(ClickYProperty, value);
    }
}

public sealed class CompizTransitionEffectFactory :
    ISkiaEffectFactory<CompizTransitionEffect>,
    ISkiaShaderEffectFactory<CompizTransitionEffect>,
    ISkiaEffectValueFactory,
    ISkiaShaderEffectValueFactory
{
    private const int KindIndex = 0;
    private const int FromImageIndex = 1;
    private const int ToImageIndex = 2;
    private const int ProgressIndex = 3;
    private const int TimeIndex = 4;
    private const int ClickXIndex = 5;
    private const int ClickYIndex = 6;

    private const string DissolveShaderSource =
        """
        uniform shader fromImage;
        uniform shader toImage;
        uniform float width;
        uniform float height;
        uniform float progress;
        uniform float time;
        uniform float clickX;
        uniform float clickY;

        float hash(float2 p) {
            return fract(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
        }

        float noise(float2 p) {
            float2 cell = floor(p);
            float2 local = fract(p);
            local = local * local * (3.0 - (2.0 * local));
            float a = hash(cell);
            float b = hash(cell + float2(1.0, 0.0));
            float c = hash(cell + float2(0.0, 1.0));
            float d = hash(cell + float2(1.0, 1.0));
            return mix(mix(a, b, local.x), mix(c, d, local.x), local.y);
        }

        float layeredNoise(float2 p) {
            float value = 0.0;
            float amplitude = 0.6;
            for (int i = 0; i < 3; i++) {
                value += noise(p) * amplitude;
                p = (p * 2.07) + float2(9.1, -5.3);
                amplitude *= 0.5;
            }
            return value;
        }

        half4 main(float2 coord) {
            float safeWidth = max(width, 1.0);
            float safeHeight = max(height, 1.0);
            float2 uv = float2(coord.x / safeWidth, coord.y / safeHeight);
            float2 click = float2(clickX, clickY);
            float distanceField = distance(uv, click);
            float pattern = layeredNoise((uv * 6.0) + float2(time * 0.11, -time * 0.07));
            float frontier = (progress * 1.35) - (distanceField * 0.42);
            float reveal = smoothstep(frontier - 0.1, frontier + 0.08, pattern);
            float glow = smoothstep(frontier - 0.02, frontier + 0.02, pattern) -
                         smoothstep(frontier + 0.02, frontier + 0.1, pattern);

            half4 fromColor = fromImage.eval(coord);
            half4 toColor = toImage.eval(coord);
            half3 rgb = mix(fromColor.rgb, toColor.rgb, half(reveal));
            rgb += half3(1.0, 0.58, 0.18) * half(glow * 0.3);
            return half4(clamp(rgb, 0.0, 1.0), 1.0);
        }
        """;

    private const string BurnShaderSource =
        """
        uniform shader fromImage;
        uniform shader toImage;
        uniform float width;
        uniform float height;
        uniform float progress;
        uniform float time;
        uniform float clickX;
        uniform float clickY;

        float hash(float2 p) {
            return fract(sin(dot(p, float2(91.7, 283.3))) * 43758.5453);
        }

        float noise(float2 p) {
            float2 cell = floor(p);
            float2 local = fract(p);
            local = local * local * (3.0 - (2.0 * local));
            float a = hash(cell);
            float b = hash(cell + float2(1.0, 0.0));
            float c = hash(cell + float2(0.0, 1.0));
            float d = hash(cell + float2(1.0, 1.0));
            return mix(mix(a, b, local.x), mix(c, d, local.x), local.y);
        }

        float fbm(float2 p) {
            float value = 0.0;
            float amplitude = 0.55;
            for (int i = 0; i < 4; i++) {
                value += noise(p) * amplitude;
                p = (p * 2.03) + float2(7.4, -6.8);
                amplitude *= 0.52;
            }
            return value;
        }

        half4 main(float2 coord) {
            float safeWidth = max(width, 1.0);
            float safeHeight = max(height, 1.0);
            float2 uv = float2(coord.x / safeWidth, coord.y / safeHeight);
            float2 click = float2(clickX, clickY);
            float distanceField = distance(uv, click);
            float field = fbm((uv * 7.0) + float2(time * 0.18, -time * 0.09));
            float front = (progress * 1.7) - (distanceField * 1.15) + (field * 0.28);
            float ember = clamp(1.0 - abs(front / 0.12), 0.0, 1.0);
            float charAmount = smoothstep(-0.02, 0.24, front) * (1.0 - smoothstep(0.24, 0.48, front));
            float reveal = smoothstep(0.18, 0.42, front);
            float smoke = smoothstep(-0.34, -0.06, front) * (1.0 - smoothstep(-0.06, 0.12, front));

            float distortion = ember * (field - 0.5) * 22.0;
            half4 fromColor = fromImage.eval(coord + float2(distortion, distortion * 0.35));
            half4 toColor = toImage.eval(coord);

            half3 rgb = fromColor.rgb;
            rgb = mix(rgb, fromColor.rgb * half3(0.2, 0.08, 0.03), half(charAmount));
            rgb = mix(rgb, toColor.rgb, half(reveal));
            rgb += half3(1.0, 0.64, 0.18) * half(ember * 0.72);
            rgb += half3(0.15, 0.12, 0.11) * half(smoke * 0.22);
            return half4(clamp(rgb, 0.0, 1.0), 1.0);
        }
        """;

    private const string CubeShaderSource =
        """
        uniform shader fromImage;
        uniform shader toImage;
        uniform float width;
        uniform float height;
        uniform float progress;
        uniform float time;
        uniform float clickX;
        uniform float clickY;

        const float PI = 3.14159265359;

        half4 main(float2 coord) {
            float safeWidth = max(width, 1.0);
            float safeHeight = max(height, 1.0);
            float aspect = safeWidth / safeHeight;
            float direction = clickX > 0.5 ? 1.0 : -1.0;
            float halfWidth = aspect;
            float halfHeight = 1.0;
            float focalLength = 3.0;
            float baseCameraDistance = focalLength - halfWidth;
            float turn = progress * progress * (3.0 - (2.0 * progress));
            float angle = turn * PI * 0.5 * direction;
            float cosine = cos(angle);
            float sine = sin(angle);
            float cameraPullback = sin(turn * PI);
            float cameraDistance = baseCameraDistance + (1.5 * cameraPullback);

            float2 screen = float2(
                ((coord.x / safeWidth) * 2.0) - 1.0,
                ((coord.y / safeHeight) * 2.0) - 1.0);
            screen.x *= aspect;

            float3 rayOrigin = float3(0.0, 0.0, cameraDistance);
            float3 rayDirection = normalize(float3(screen.x, screen.y, -focalLength));
            float3 rotatedDirection = float3(
                (rayDirection.x * cosine) - (rayDirection.z * sine),
                rayDirection.y,
                (rayDirection.x * sine) + (rayDirection.z * cosine));
            float3 rotatedOrigin = float3(
                (rayOrigin.x * cosine) - (rayOrigin.z * sine),
                rayOrigin.y,
                (rayOrigin.x * sine) + (rayOrigin.z * cosine));

            half3 rgb = half3(0.0);
            float nearestHit = 1e10;

            if (abs(rotatedDirection.z) > 0.001) {
                float hitDistance = (-halfWidth - rotatedOrigin.z) / rotatedDirection.z;
                if (hitDistance > 0.0 && hitDistance < nearestHit) {
                    float3 hitPoint = rotatedOrigin + (hitDistance * rotatedDirection);
                    if (abs(hitPoint.x) <= halfWidth && abs(hitPoint.y) <= halfHeight) {
                        nearestHit = hitDistance;
                        float2 sampleUv = float2(
                            ((hitPoint.x / halfWidth) * 0.5) + 0.5,
                            (hitPoint.y * 0.5) + 0.5);
                        half4 sample = fromImage.eval(float2(sampleUv.x * safeWidth, sampleUv.y * safeHeight));
                        float facing = max(0.0, cosine);
                        rgb = sample.rgb * half(0.25 + (0.75 * facing));
                    }
                }
            }

            if (direction > 0.0 && abs(rotatedDirection.x) > 0.001) {
                float hitDistance = (halfWidth - rotatedOrigin.x) / rotatedDirection.x;
                if (hitDistance > 0.0 && hitDistance < nearestHit) {
                    float3 hitPoint = rotatedOrigin + (hitDistance * rotatedDirection);
                    if (abs(hitPoint.z) <= halfWidth && abs(hitPoint.y) <= halfHeight) {
                        nearestHit = hitDistance;
                        float2 sampleUv = float2(
                            ((hitPoint.z / halfWidth) * 0.5) + 0.5,
                            (hitPoint.y * 0.5) + 0.5);
                        half4 sample = toImage.eval(float2(sampleUv.x * safeWidth, sampleUv.y * safeHeight));
                        float facing = max(0.0, sine * direction);
                        rgb = sample.rgb * half(0.25 + (0.75 * facing));
                    }
                }
            }

            if (direction < 0.0 && abs(rotatedDirection.x) > 0.001) {
                float hitDistance = (-halfWidth - rotatedOrigin.x) / rotatedDirection.x;
                if (hitDistance > 0.0 && hitDistance < nearestHit) {
                    float3 hitPoint = rotatedOrigin + (hitDistance * rotatedDirection);
                    if (abs(hitPoint.z) <= halfWidth && abs(hitPoint.y) <= halfHeight) {
                        float2 sampleUv = float2(
                            ((-hitPoint.z / halfWidth) * 0.5) + 0.5,
                            (hitPoint.y * 0.5) + 0.5);
                        half4 sample = toImage.eval(float2(sampleUv.x * safeWidth, sampleUv.y * safeHeight));
                        float facing = max(0.0, -sine * direction);
                        rgb = sample.rgb * half(0.25 + (0.75 * facing));
                    }
                }
            }

            return half4(clamp(rgb, 0.0, 1.0), 1.0);
        }
        """;

    private const string WobblyShaderSource =
        """
        uniform shader fromImage;
        uniform shader toImage;
        uniform float width;
        uniform float height;
        uniform float progress;
        uniform float time;
        uniform float clickX;
        uniform float clickY;

        half4 main(float2 coord) {
            float safeWidth = max(width, 1.0);
            float safeHeight = max(height, 1.0);
            float2 uv = float2(coord.x / safeWidth, coord.y / safeHeight);
            float2 click = float2(clickX, clickY);
            float2 direction = normalize((uv - click) + float2(0.0001, 0.0001));
            float distanceField = distance(uv, click);
            float front = (progress * 1.35) - distanceField;
            float ring = exp(-pow(front * 18.0, 2.0));
            float oscillation = sin((distanceField * 44.0) - (time * 7.0));
            float displacement = ring * (1.0 - progress) * (18.0 + (oscillation * 7.0));
            float2 offset = direction * displacement;

            half4 fromColor = fromImage.eval(coord + offset);
            half4 toColor = toImage.eval(coord - (offset * 0.35));
            float reveal = smoothstep(-0.03, 0.16, front);

            half3 rgb = mix(fromColor.rgb, toColor.rgb, half(reveal));
            rgb += half3(0.26, 0.42, 0.92) * half(ring * (1.0 - progress) * 0.22);
            return half4(clamp(rgb, 0.0, 1.0), 1.0);
        }
        """;

    private const string GenieShaderSource =
        """
        uniform shader fromImage;
        uniform shader toImage;
        uniform float width;
        uniform float height;
        uniform float progress;
        uniform float time;
        uniform float clickX;
        uniform float clickY;

        half4 main(float2 coord) {
            float safeWidth = max(width, 1.0);
            float safeHeight = max(height, 1.0);
            float2 click = float2(clickX * safeWidth, clickY * safeHeight);
            float t = progress * progress;
            float rowInfluence = clamp(1.0 - abs((coord.y / safeHeight) - clickY), 0.0, 1.0);
            float squeeze = max(1.0 - (t * (0.76 + (rowInfluence * 0.24))), 0.05);
            float centerX = mix(coord.x, click.x, t);
            float sourceX = ((coord.x - centerX) / squeeze) + click.x;
            float sourceY = mix(coord.y, click.y, t * t);
            float curve = sin((coord.y / safeHeight) * 3.14159265) * (1.0 - progress) * 24.0;
            sourceX += curve * sign(coord.x - click.x);

            bool valid = sourceX >= 0.0 && sourceX <= safeWidth && sourceY >= 0.0 && sourceY <= safeHeight;
            half4 fromColor = valid ? fromImage.eval(float2(sourceX, sourceY)) : half4(0.0);
            half4 toColor = toImage.eval(coord);
            float reveal = smoothstep(0.34, 1.0, progress);

            half3 rgb = mix(fromColor.rgb, toColor.rgb, half(reveal));
            float glow = exp(-distance(coord, click) / max(min(safeWidth, safeHeight) * 0.18, 1.0)) * progress * 0.18;
            rgb += half3(0.52, 0.34, 0.92) * half(glow);
            return half4(clamp(rgb, 0.0, 1.0), 1.0);
        }
        """;

    private const string MagneticShaderSource =
        """
        uniform shader fromImage;
        uniform shader toImage;
        uniform float width;
        uniform float height;
        uniform float progress;
        uniform float time;
        uniform float clickX;
        uniform float clickY;

        float hash(float2 p) {
            return fract(sin(dot(p, float2(73.1, 187.7))) * 43758.5453);
        }

        half4 main(float2 coord) {
            float safeWidth = max(width, 1.0);
            float safeHeight = max(height, 1.0);
            float2 uv = float2(coord.x / safeWidth, coord.y / safeHeight);
            float2 click = float2(clickX, clickY);
            float scatter = smoothstep(0.0, 0.5, progress);
            float assemble = smoothstep(0.5, 1.0, progress);
            float2 toClick = click - uv;
            float distanceField = length(toClick);
            float2 direction = normalize(toClick + float2(0.001, 0.001));
            float pull = exp(-(distanceField * distanceField) * 4.0);
            float scatterAmount = scatter * pull * 0.4;
            float turbulence = (hash((uv * 100.0) + time) - 0.5) * 0.04 * scatter;

            float2 uvR = clamp(uv + (direction * (scatterAmount * 1.15)) + float2(turbulence, turbulence), 0.0, 1.0);
            float2 uvG = clamp(uv + (direction * scatterAmount) + float2(turbulence, turbulence), 0.0, 1.0);
            float2 uvB = clamp(uv + (direction * (scatterAmount * 0.85)) + float2(turbulence, turbulence), 0.0, 1.0);

            half3 scatteredFrom = half3(
                fromImage.eval(float2(uvR.x * safeWidth, uvR.y * safeHeight)).r,
                fromImage.eval(float2(uvG.x * safeWidth, uvG.y * safeHeight)).g,
                fromImage.eval(float2(uvB.x * safeWidth, uvB.y * safeHeight)).b);

            half4 toColor = toImage.eval(coord);
            float assembleRadius = assemble * 2.0;
            float reveal = 1.0 - smoothstep(assembleRadius - 0.3, assembleRadius, distanceField);
            float cellNoise = hash(floor((uv * float2(safeWidth, safeHeight)) / 4.0));
            float dissolve = (1.0 - scatter) * step(scatter * 1.2, cellNoise + 0.2);

            half3 rgb = scatteredFrom * half(dissolve);
            rgb = mix(rgb, toColor.rgb, half(reveal));

            float energy = exp(-(distanceField * distanceField) * 10.0) * ((sin(time * 8.0) * 0.5) + 0.5);
            float energyAmount = scatter * (1.0 - assemble);
            rgb += half3(0.3, 0.5, 1.0) * half(energy * energyAmount * 0.4);

            float edgeGlow = smoothstep(assembleRadius - 0.05, assembleRadius, distanceField) *
                             (1.0 - smoothstep(assembleRadius, assembleRadius + 0.05, distanceField));
            rgb += half3(0.5, 0.7, 1.0) * half(edgeGlow * 0.8);
            return half4(clamp(rgb, 0.0, 1.0), 1.0);
        }
        """;

    public Thickness GetPadding(CompizTransitionEffect effect) => default;

    public SKImageFilter? CreateFilter(CompizTransitionEffect effect, SkiaEffectContext context) => null;

    public Thickness GetPadding(object[] values) => default;

    public SKImageFilter? CreateFilter(object[] values, SkiaEffectContext context) => null;

    public SkiaShaderEffect? CreateShaderEffect(CompizTransitionEffect effect, SkiaShaderEffectContext context) =>
        CreateShaderEffect(
            new object[]
            {
                effect.Kind,
                effect.FromImage,
                effect.ToImage,
                effect.Progress,
                effect.Time,
                effect.ClickX,
                effect.ClickY
            },
            context);

    public SkiaShaderEffect? CreateShaderEffect(object[] values, SkiaShaderEffectContext context)
    {
        if (!TryAcquireImages(values, out var fromLease, out var toLease))
        {
            return null;
        }

        try
        {
            return ((CompizTransitionKind)values[KindIndex]) switch
            {
                CompizTransitionKind.Burn => CreateTransitionShader(values, context, fromLease, toLease, BurnShaderSource, DrawBurnFallback),
                CompizTransitionKind.Cube => CreateTransitionShader(values, context, fromLease, toLease, CubeShaderSource, DrawCubeFallback),
                CompizTransitionKind.Wobbly => CreateTransitionShader(values, context, fromLease, toLease, WobblyShaderSource, DrawWobblyFallback),
                CompizTransitionKind.Genie => CreateTransitionShader(values, context, fromLease, toLease, GenieShaderSource, DrawGenieFallback),
                CompizTransitionKind.Magnetic => CreateTransitionShader(values, context, fromLease, toLease, MagneticShaderSource, DrawMagneticFallback),
                _ => CreateTransitionShader(values, context, fromLease, toLease, DissolveShaderSource, DrawDissolveFallback)
            };
        }
        catch
        {
            fromLease.Dispose();
            toLease.Dispose();
            throw;
        }
    }

    private delegate void TransitionFallbackRenderer(
        SKCanvas canvas,
        SKRect rect,
        SKImage fromImage,
        SKImage toImage,
        float progress,
        float time,
        SKPoint click);

    private static SkiaShaderEffect CreateTransitionShader(
        object[] values,
        SkiaShaderEffectContext context,
        SkiaShaderImageLease fromLease,
        SkiaShaderImageLease toLease,
        string shaderSource,
        TransitionFallbackRenderer fallbackRenderer)
    {
        var progress = Clamp01((double)values[ProgressIndex]);
        var time = Math.Max(0f, (float)(double)values[TimeIndex]);
        var click = GetClick(values);

        return SkiaRuntimeShaderBuilder.Create(
            shaderSource,
            context,
            uniforms =>
            {
                uniforms.Add("width", context.EffectBounds.Width);
                uniforms.Add("height", context.EffectBounds.Height);
                uniforms.Add("progress", progress);
                uniforms.Add("time", time);
                uniforms.Add("clickX", click.X);
                uniforms.Add("clickY", click.Y);
            },
            fallbackRenderer: (canvas, _, rect) =>
            {
                fallbackRenderer(canvas, rect, fromLease.Image, toLease.Image, progress, time, click);
            },
            blendMode: SKBlendMode.Src,
            configureOwnedChildren: (children, _, ownedResources) =>
            {
                var fromShader = fromLease.Image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
                var toShader = toLease.Image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
                children.Add("fromImage", fromShader);
                children.Add("toImage", toShader);
                ownedResources.Add(fromShader);
                ownedResources.Add(toShader);
            },
            ownedResources: new IDisposable[] { fromLease, toLease });
    }

    private static bool TryAcquireImages(object[] values, out SkiaShaderImageLease fromLease, out SkiaShaderImageLease toLease)
    {
        fromLease = null!;
        toLease = null!;

        if (!SkiaShaderImageRegistry.TryAcquire((SkiaShaderImageHandle)values[FromImageIndex], out var from) || from is null)
        {
            return false;
        }

        if (!SkiaShaderImageRegistry.TryAcquire((SkiaShaderImageHandle)values[ToImageIndex], out var to) || to is null)
        {
            from.Dispose();
            return false;
        }

        fromLease = from;
        toLease = to;
        return true;
    }

    private static SKPoint GetClick(object[] values) =>
        new(
            Clamp01((double)values[ClickXIndex]),
            Clamp01((double)values[ClickYIndex]));

    private static float Clamp01(double value) => (float)Math.Clamp(value, 0d, 1d);

    private static void DrawDissolveFallback(SKCanvas canvas, SKRect rect, SKImage fromImage, SKImage toImage, float progress, float time, SKPoint click)
    {
        using var clipScope = new CanvasClipScope(canvas, rect);
        DrawImage(canvas, fromImage, rect);

        var cellSize = MathF.Max(MathF.Min(rect.Width, rect.Height) / 18f, 14f);
        var glow = new SKColor(255, 165, 74, 72);
        for (var y = rect.Top; y < rect.Bottom; y += cellSize)
        {
            for (var x = rect.Left; x < rect.Right; x += cellSize)
            {
                var cellRect = new SKRect(x, y, MathF.Min(x + cellSize, rect.Right), MathF.Min(y + cellSize, rect.Bottom));
                var localCenter = new SKPoint(
                    (cellRect.MidX - rect.Left) / MathF.Max(rect.Width, 1f),
                    (cellRect.MidY - rect.Top) / MathF.Max(rect.Height, 1f));
                var threshold = (progress * 1.25f) - (Distance(localCenter, click) * 0.35f);
                var noise = Hash(cellRect.Left, cellRect.Top, time * 8f);
                if (noise <= threshold)
                {
                    DrawImageCell(canvas, toImage, rect, cellRect);
                }

                if (MathF.Abs(noise - threshold) < 0.05f)
                {
                    using var glowPaint = new SKPaint
                    {
                        Color = glow,
                        IsAntialias = false,
                        Style = SKPaintStyle.Fill
                    };
                    canvas.DrawRect(cellRect, glowPaint);
                }
            }
        }
    }

    private static void DrawBurnFallback(SKCanvas canvas, SKRect rect, SKImage fromImage, SKImage toImage, float progress, float time, SKPoint click)
    {
        using var clipScope = new CanvasClipScope(canvas, rect);
        DrawImage(canvas, fromImage, rect);

        var clickPoint = ToAbsolutePoint(rect, click);
        var radius = MathF.Max(rect.Width, rect.Height) * (0.12f + (progress * 1.05f));
        using var revealPath = new SKPath();
        revealPath.AddCircle(clickPoint.X, clickPoint.Y, radius);
        canvas.Save();
        canvas.ClipPath(revealPath, antialias: true);
        DrawImage(canvas, toImage, rect);
        canvas.Restore();

        using var emberPaint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(
                clickPoint,
                MathF.Max(radius * 0.22f, 1f),
                new[]
                {
                    new SKColor(255, 245, 214, 192),
                    new SKColor(255, 136, 47, 140),
                    new SKColor(56, 18, 8, 0)
                },
                null,
                SKShaderTileMode.Clamp)
        };
        canvas.DrawCircle(clickPoint, radius + 6f, emberPaint);

        using var smokePaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(33, 26, 24, (byte)(Math.Clamp(1f - progress, 0f, 1f) * 48f))
        };
        canvas.DrawCircle(clickPoint, radius * (0.7f + (Hash(clickPoint.X, clickPoint.Y, time * 12f) * 0.18f)), smokePaint);
    }

    private static void DrawCubeFallback(SKCanvas canvas, SKRect rect, SKImage fromImage, SKImage toImage, float progress, float time, SKPoint click)
    {
        using var clipScope = new CanvasClipScope(canvas, rect);
        using var backgroundPaint = new SKPaint
        {
            Color = new SKColor(10, 14, 24, 255),
            IsAntialias = true
        };
        canvas.DrawRect(rect, backgroundPaint);

        var direction = click.X >= 0.5f ? 1f : -1f;
        var eased = Ease(progress);
        var outgoingWidth = MathF.Max(rect.Width * (1f - (eased * 0.92f)), rect.Width * 0.08f);
        var incomingWidth = MathF.Max(rect.Width * (0.08f + (eased * 0.92f)), rect.Width * 0.08f);
        var fromRect = direction > 0f
            ? new SKRect(rect.Left, rect.Top + (eased * 14f), rect.Left + outgoingWidth, rect.Bottom - (eased * 14f))
            : new SKRect(rect.Right - outgoingWidth, rect.Top + (eased * 14f), rect.Right, rect.Bottom - (eased * 14f));
        var toRect = direction > 0f
            ? new SKRect(rect.Right - incomingWidth, rect.Top + ((1f - eased) * 14f), rect.Right, rect.Bottom - ((1f - eased) * 14f))
            : new SKRect(rect.Left, rect.Top + ((1f - eased) * 14f), rect.Left + incomingWidth, rect.Bottom - ((1f - eased) * 14f));

        DrawImage(canvas, fromImage, fromRect);
        DrawTint(canvas, fromRect, new SKColor(0, 0, 0, (byte)(Math.Clamp(progress, 0f, 1f) * 82f)));

        DrawImage(canvas, toImage, toRect);
        DrawTint(canvas, toRect, new SKColor(255, 255, 255, (byte)(Math.Clamp(1f - progress, 0f, 1f) * 24f)));

        using var seamPaint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(direction > 0f ? fromRect.Right : toRect.Right, rect.Top),
                new SKPoint(direction > 0f ? toRect.Left : fromRect.Left, rect.Top),
                new[]
                {
                    new SKColor(0, 0, 0, 180),
                    new SKColor(0, 0, 0, 0)
                },
                null,
                SKShaderTileMode.Clamp)
        };
        canvas.DrawRect(rect, seamPaint);
    }

    private static void DrawWobblyFallback(SKCanvas canvas, SKRect rect, SKImage fromImage, SKImage toImage, float progress, float time, SKPoint click)
    {
        using var clipScope = new CanvasClipScope(canvas, rect);
        DrawImage(canvas, fromImage, rect);

        var clickPoint = ToAbsolutePoint(rect, click);
        var radius = MathF.Max(rect.Width, rect.Height) * (0.08f + (progress * 0.92f));
        using var revealPath = new SKPath();
        revealPath.AddCircle(clickPoint.X, clickPoint.Y, radius + (MathF.Sin(time * 9f) * 10f));

        canvas.Save();
        canvas.ClipPath(revealPath, antialias: true);
        DrawImage(canvas, toImage, rect);
        canvas.Restore();

        using var ringPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = MathF.Max(MathF.Min(rect.Width, rect.Height) * 0.02f, 2f),
            Color = new SKColor(102, 153, 255, (byte)(Math.Clamp(1f - progress, 0f, 1f) * 160f))
        };
        canvas.DrawCircle(clickPoint, radius, ringPaint);
    }

    private static void DrawGenieFallback(SKCanvas canvas, SKRect rect, SKImage fromImage, SKImage toImage, float progress, float time, SKPoint click)
    {
        using var clipScope = new CanvasClipScope(canvas, rect);
        DrawImage(canvas, toImage, rect);

        var clickPoint = ToAbsolutePoint(rect, click);
        var eased = progress * progress;
        var squeezeWidth = MathF.Max(rect.Width * (1f - (eased * 0.9f)), 20f);
        var squeezeHeight = MathF.Max(rect.Height * (1f - (eased * 0.92f)), 26f);
        var fromRect = new SKRect(
            clickPoint.X - (squeezeWidth * 0.5f),
            clickPoint.Y - (squeezeHeight * 0.5f),
            clickPoint.X + (squeezeWidth * 0.5f),
            clickPoint.Y + (squeezeHeight * 0.5f));
        DrawImage(canvas, fromImage, fromRect, 1f - (progress * 0.68f));

        using var glowPaint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(
                clickPoint,
                MathF.Max(MathF.Min(rect.Width, rect.Height) * 0.18f, 10f),
                new[]
                {
                    new SKColor(155, 105, 255, (byte)(Math.Clamp(progress, 0f, 1f) * 128f)),
                    new SKColor(155, 105, 255, 0)
                },
                null,
                SKShaderTileMode.Clamp)
        };
        canvas.DrawCircle(clickPoint, MathF.Max(MathF.Min(rect.Width, rect.Height) * 0.18f, 10f), glowPaint);
    }

    private static void DrawMagneticFallback(SKCanvas canvas, SKRect rect, SKImage fromImage, SKImage toImage, float progress, float time, SKPoint click)
    {
        using var clipScope = new CanvasClipScope(canvas, rect);
        var clickPoint = ToAbsolutePoint(rect, click);
        var scale = 1f - (progress * 0.24f);
        var fromRect = SKRect.Create(
            clickPoint.X - ((rect.Width * scale) * 0.5f),
            clickPoint.Y - ((rect.Height * scale) * 0.5f),
            rect.Width * scale,
            rect.Height * scale);
        DrawImage(canvas, fromImage, fromRect, 1f - (progress * 0.42f));

        var revealRadius = MathF.Max(rect.Width, rect.Height) * MathF.Max(progress - 0.2f, 0f) * 1.1f;
        if (revealRadius > 0f)
        {
            using var revealPath = new SKPath();
            revealPath.AddCircle(clickPoint.X, clickPoint.Y, revealRadius);
            canvas.Save();
            canvas.ClipPath(revealPath, antialias: true);
            DrawImage(canvas, toImage, rect);
            canvas.Restore();
        }

        using var sparkPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(90, 160, 255, (byte)(Math.Clamp(1f - progress, 0f, 1f) * 180f))
        };
        for (var index = 0; index < 24; index++)
        {
            var angle = (index / 24f) * MathF.PI * 2f;
            var orbit = (18f + (Hash(index, time * 16f, clickPoint.X) * 42f)) * (1f - progress);
            var particle = new SKPoint(
                clickPoint.X + (MathF.Cos(angle) * orbit),
                clickPoint.Y + (MathF.Sin(angle) * orbit));
            canvas.DrawCircle(particle, 1.6f + (Hash(index, clickPoint.Y, time * 9f) * 2.4f), sparkPaint);
        }
    }

    private static void DrawImage(SKCanvas canvas, SKImage image, SKRect rect, float opacity = 1f)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(255, 255, 255, (byte)(Math.Clamp(opacity, 0f, 1f) * 255f))
        };
        canvas.DrawImage(image, rect, paint);
    }

    private static void DrawTint(SKCanvas canvas, SKRect rect, SKColor color)
    {
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = color
        };
        canvas.DrawRect(rect, paint);
    }

    private static void DrawImageCell(SKCanvas canvas, SKImage image, SKRect fullRect, SKRect cellRect)
    {
        var sourceRect = new SKRect(
            ((cellRect.Left - fullRect.Left) / MathF.Max(fullRect.Width, 1f)) * image.Width,
            ((cellRect.Top - fullRect.Top) / MathF.Max(fullRect.Height, 1f)) * image.Height,
            ((cellRect.Right - fullRect.Left) / MathF.Max(fullRect.Width, 1f)) * image.Width,
            ((cellRect.Bottom - fullRect.Top) / MathF.Max(fullRect.Height, 1f)) * image.Height);

        using var paint = new SKPaint
        {
            IsAntialias = false
        };
        canvas.DrawImage(image, sourceRect, cellRect, paint);
    }

    private static float Ease(float value) =>
        value < 0.5f
            ? 2f * value * value
            : 1f - (MathF.Pow((-2f * value) + 2f, 2f) * 0.5f);

    private static float Distance(SKPoint left, SKPoint right)
    {
        var dx = left.X - right.X;
        var dy = left.Y - right.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    private static SKPoint ToAbsolutePoint(SKRect rect, SKPoint normalized) =>
        new(
            rect.Left + (rect.Width * normalized.X),
            rect.Top + (rect.Height * normalized.Y));

    private static float Hash(float x, float y, float z) =>
        MathF.Abs(MathF.Sin((x * 12.9898f) + (y * 78.233f) + (z * 37.719f))) % 1f;

    private readonly ref struct CanvasClipScope
    {
        private readonly SKCanvas _canvas;
        private readonly int _restoreCount;

        public CanvasClipScope(SKCanvas canvas, SKRect rect)
        {
            _canvas = canvas;
            _restoreCount = canvas.Save();
            canvas.ClipRect(rect, antialias: true);
        }

        public void Dispose()
        {
            _canvas.RestoreToCount(_restoreCount);
        }
    }
}
