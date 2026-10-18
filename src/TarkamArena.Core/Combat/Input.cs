using System.Runtime.CompilerServices;

namespace TarkamArena.Core.Combat;

/// <summary>
/// One frame of controller input. Directions are absolute (screen left/right); the simulation
/// converts them to forward/back using the fighter's facing.
/// </summary>
[Flags]
public enum InputButtons : ushort
{
    None = 0,
    Up = 1 << 0,
    Down = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
    Light = 1 << 4,
    Heavy = 1 << 5,
    Grab = 1 << 6,
    Block = 1 << 7,
    Dodge = 1 << 8,
    Special = 1 << 9,
    Tag = 1 << 10,
    Rage = 1 << 11,

    Directions = Up | Down | Left | Right,
    Attacks = Light | Heavy | Grab | Special,
}

/// <summary>Input packed into the 16 bits that go over the wire every frame.</summary>
public readonly record struct InputFrame(InputButtons Buttons)
{
    public static readonly InputFrame Neutral = new(InputButtons.None);

    public bool Has(InputButtons b) => (Buttons & b) != 0;

    public ushort Pack() => (ushort)Buttons;

    public static InputFrame Unpack(ushort raw) => new((InputButtons)raw);
}

/// <summary>Facing-relative input token used for motion inputs and finisher sequences.</summary>
public enum InputToken : byte
{
    Up,
    Down,
    Forward,
    Back,
    Light,
    Heavy,
    Grab,
    Special,
}

/// <summary>
/// Ring buffer of the last 32 frames of raw input. It is an inline array so a
/// <see cref="Match.FighterState"/> stays a blittable value that a rollback snapshot can copy.
/// </summary>
[InlineArray(Length)]
public struct InputHistory
{
    public const int Length = 32;
    private ushort _element;
}

/// <summary>Ring buffer of the last eight pressed tokens, for finisher sequences.</summary>
[InlineArray(Length)]
public struct TokenHistory
{
    public const int Length = 8;
    private byte _element;
}

public static class InputTokens
{
    public static char ToChar(InputToken token) => token switch
    {
        InputToken.Up => 'U',
        InputToken.Down => 'D',
        InputToken.Forward => 'F',
        InputToken.Back => 'B',
        InputToken.Light => 'L',
        InputToken.Heavy => 'H',
        InputToken.Grab => 'G',
        _ => 'S',
    };

    public static InputToken FromChar(char c) => c switch
    {
        'U' => InputToken.Up,
        'D' => InputToken.Down,
        'F' => InputToken.Forward,
        'B' => InputToken.Back,
        'L' => InputToken.Light,
        'H' => InputToken.Heavy,
        'G' => InputToken.Grab,
        'S' => InputToken.Special,
        _ => throw new ArgumentOutOfRangeException(nameof(c), c, "Unknown input token."),
    };

    /// <summary>Human-readable glyphs for move lists, e.g. "↓ → ↓ + H".</summary>
    public static string ToGlyph(InputToken token) => token switch
    {
        InputToken.Up => "↑",
        InputToken.Down => "↓",
        InputToken.Forward => "→",
        InputToken.Back => "←",
        InputToken.Light => "L",
        InputToken.Heavy => "H",
        InputToken.Grab => "G",
        _ => "S",
    };
}
