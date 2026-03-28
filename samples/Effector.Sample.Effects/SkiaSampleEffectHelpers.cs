using Avalonia;
using Avalonia.Media;
using SkiaSharp;

namespace Effector.Sample.Effects;

internal static class SkiaSampleEffectHelpers
{
    public static double Clamp(double value, double minimum, double maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }

    public static float Clamp01(double value) => (float)Clamp(value, 0d, 1d);

    public static SKColor ToSkColor(Color color, double opacity = 1d)
    {
        var alpha = Clamp(color.A * opacity, 0d, 255d);
        return new SKColor(color.R, color.G, color.B, (byte)alpha);
    }

    public static Thickness UniformPadding(double radius) => new(System.Math.Ceiling(System.Math.Max(0d, radius)) + 1d);

    public static SKImageFilter IdentityFilter() =>
        SkiaFilterBuilder.ColorFilter(SKColorFilter.CreateColorMatrix(ColorMatrixBuilder.CreateIdentity()))!;
}
