using System;
using System.Globalization;

namespace Effector;

public readonly struct SkiaShaderImageHandle : IEquatable<SkiaShaderImageHandle>
{
    public SkiaShaderImageHandle(long value)
    {
        Value = value;
    }

    public long Value { get; }

    public bool IsEmpty => Value == 0;

    public bool Equals(SkiaShaderImageHandle other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is SkiaShaderImageHandle other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() =>
        IsEmpty
            ? "empty"
            : Value.ToString(CultureInfo.InvariantCulture);

    public static bool operator ==(SkiaShaderImageHandle left, SkiaShaderImageHandle right) => left.Equals(right);

    public static bool operator !=(SkiaShaderImageHandle left, SkiaShaderImageHandle right) => !left.Equals(right);
}
