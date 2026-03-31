using Avalonia;
using SkiaSharp;

namespace Effector;

public interface ISkiaEffectFactory<in TEffect>
    where TEffect : class
{
    Thickness GetPadding(TEffect effect);

    SKImageFilter? CreateFilter(TEffect effect, SkiaEffectContext context);
}
