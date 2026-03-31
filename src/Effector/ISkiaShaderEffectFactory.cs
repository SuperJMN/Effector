namespace Effector;

public interface ISkiaShaderEffectFactory<in TEffect>
    where TEffect : class
{
    SkiaShaderEffect? CreateShaderEffect(TEffect effect, SkiaShaderEffectContext context);
}
