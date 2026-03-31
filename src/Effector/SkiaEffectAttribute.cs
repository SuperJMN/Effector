using System;

namespace Effector;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SkiaEffectAttribute : Attribute
{
    public SkiaEffectAttribute(Type factoryType)
    {
        FactoryType = factoryType ?? throw new ArgumentNullException(nameof(factoryType));
    }

    public Type FactoryType { get; }

    public string? Name { get; set; }

    // Effects that need SVG-style SourceGraphic semantics can opt into an
    // explicit capture path instead of relying on Skia's live SaveLayer input.
    public bool RequiresSourceCapture { get; set; }
}
