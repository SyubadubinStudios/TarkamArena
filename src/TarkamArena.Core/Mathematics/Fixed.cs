using System.Runtime.CompilerServices;

namespace TarkamArena.Core.Mathematics;

/// <summary>
/// 16.16 fixed-point number. The fight simulation uses it instead of <see cref="float"/> so that
/// every machine computes bit-identical results, which rollback netcode depends on.
/// </summary>
public readonly struct Fixed : IEquatable<Fixed>, IComparable<Fixed>
{
    public const int FractionBits = 16;
    public const int OneRaw = 1 << FractionBits;

    public readonly int Raw;

    private Fixed(int raw) => Raw = raw;

    public static readonly Fixed Zero;
    public static readonly Fixed One = new(OneRaw);
    public static readonly Fixed Half = new(OneRaw / 2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fixed FromRaw(int raw) => new(raw);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fixed FromInt(int value) => new(value << FractionBits);

    /// <summary>Exact rational constant, e.g. <c>FromRatio(3, 2)</c> for 1.5.</summary>
    public static Fixed FromRatio(int numerator, int denominator) =>
        new((int)(((long)numerator << FractionBits) / denominator));

    /// <summary>For authoring content only; never call inside the simulation loop.</summary>
    public static Fixed FromFloat(float value) => new((int)MathF.Round(value * OneRaw));

    public float ToFloat() => Raw / (float)OneRaw;
    public int ToInt() => Raw >> FractionBits;

    public static Fixed operator +(Fixed a, Fixed b) => new(a.Raw + b.Raw);
    public static Fixed operator -(Fixed a, Fixed b) => new(a.Raw - b.Raw);
    public static Fixed operator -(Fixed a) => new(-a.Raw);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fixed operator *(Fixed a, Fixed b) => new((int)(((long)a.Raw * b.Raw) >> FractionBits));

    public static Fixed operator *(Fixed a, int b) => new(a.Raw * b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Fixed operator /(Fixed a, Fixed b) => new((int)(((long)a.Raw << FractionBits) / b.Raw));

    public static Fixed operator /(Fixed a, int b) => new(a.Raw / b);

    public static bool operator ==(Fixed a, Fixed b) => a.Raw == b.Raw;
    public static bool operator !=(Fixed a, Fixed b) => a.Raw != b.Raw;
    public static bool operator <(Fixed a, Fixed b) => a.Raw < b.Raw;
    public static bool operator >(Fixed a, Fixed b) => a.Raw > b.Raw;
    public static bool operator <=(Fixed a, Fixed b) => a.Raw <= b.Raw;
    public static bool operator >=(Fixed a, Fixed b) => a.Raw >= b.Raw;

    public static Fixed Abs(Fixed v) => v.Raw < 0 ? new(-v.Raw) : v;
    public static Fixed Min(Fixed a, Fixed b) => a.Raw < b.Raw ? a : b;
    public static Fixed Max(Fixed a, Fixed b) => a.Raw > b.Raw ? a : b;
    public static Fixed Clamp(Fixed v, Fixed min, Fixed max) => v.Raw < min.Raw ? min : v.Raw > max.Raw ? max : v;
    public static int Sign(Fixed v) => v.Raw > 0 ? 1 : v.Raw < 0 ? -1 : 0;

    /// <summary>Moves <paramref name="value"/> towards <paramref name="target"/> by at most <paramref name="amount"/>.</summary>
    public static Fixed Approach(Fixed value, Fixed target, Fixed amount)
    {
        if (value < target) return Min(value + amount, target);
        if (value > target) return Max(value - amount, target);
        return value;
    }

    public bool Equals(Fixed other) => Raw == other.Raw;
    public override bool Equals(object? obj) => obj is Fixed f && f.Raw == Raw;
    public override int GetHashCode() => Raw;
    public int CompareTo(Fixed other) => Raw.CompareTo(other.Raw);
    public override string ToString() => ToFloat().ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}
