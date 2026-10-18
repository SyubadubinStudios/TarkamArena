namespace TarkamArena.Core.Mathematics;

/// <summary>2D fixed-point vector on the fighting plane: X runs along the stage, Y is up.</summary>
public struct FixedVector2 : IEquatable<FixedVector2>
{
    public Fixed X;
    public Fixed Y;

    public FixedVector2(Fixed x, Fixed y)
    {
        X = x;
        Y = y;
    }

    public static FixedVector2 Of(float x, float y) => new(Fixed.FromFloat(x), Fixed.FromFloat(y));

    public static FixedVector2 operator +(FixedVector2 a, FixedVector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static FixedVector2 operator -(FixedVector2 a, FixedVector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static FixedVector2 operator *(FixedVector2 a, Fixed s) => new(a.X * s, a.Y * s);

    public readonly System.Numerics.Vector2 ToVector2() => new(X.ToFloat(), Y.ToFloat());

    public readonly bool Equals(FixedVector2 other) => X == other.X && Y == other.Y;
    public override readonly bool Equals(object? obj) => obj is FixedVector2 v && Equals(v);
    public override readonly int GetHashCode() => HashCode.Combine(X.Raw, Y.Raw);
    public static bool operator ==(FixedVector2 a, FixedVector2 b) => a.Equals(b);
    public static bool operator !=(FixedVector2 a, FixedVector2 b) => !a.Equals(b);
    public override readonly string ToString() => $"({X}, {Y})";
}

/// <summary>Axis-aligned box in fixed-point, used for hitboxes, hurtboxes and pushboxes.</summary>
public readonly struct FixedBox
{
    public readonly Fixed Left;
    public readonly Fixed Bottom;
    public readonly Fixed Right;
    public readonly Fixed Top;

    public FixedBox(Fixed left, Fixed bottom, Fixed right, Fixed top)
    {
        Left = left;
        Bottom = bottom;
        Right = right;
        Top = top;
    }

    /// <summary>Builds a box from a centre offset and size relative to <paramref name="origin"/>, mirrored by <paramref name="facing"/>.</summary>
    public static FixedBox FromOffset(FixedVector2 origin, FixedVector2 offset, FixedVector2 size, int facing)
    {
        Fixed cx = origin.X + (facing >= 0 ? offset.X : -offset.X);
        Fixed cy = origin.Y + offset.Y;
        Fixed hw = size.X / 2;
        Fixed hh = size.Y / 2;
        return new FixedBox(cx - hw, cy - hh, cx + hw, cy + hh);
    }

    public bool Overlaps(in FixedBox other) =>
        Left < other.Right && Right > other.Left && Bottom < other.Top && Top > other.Bottom;

    public Fixed CenterX => (Left + Right) / 2;
    public Fixed CenterY => (Bottom + Top) / 2;
}
