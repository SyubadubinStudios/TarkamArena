using TarkamArena.Core.Combat;
using TarkamArena.Core.Localization;

namespace TarkamArena.Core.Settings;

public enum GraphicsQuality : byte
{
    Low,
    Medium,
    High,
    Ultra,
}

/// <summary>Player preferences, persisted by TarkamArena.Persistence.</summary>
public sealed class GameSettings
{
    public Language Language { get; set; } = Language.Indonesian;
    public float MasterVolume { get; set; } = 0.9f;
    public float MusicVolume { get; set; } = 0.7f;
    public float SfxVolume { get; set; } = 0.9f;
    public GraphicsQuality Quality { get; set; } = GraphicsQuality.Medium;
    public bool MotionBlur { get; set; } = true;
    public bool DynamicWeather { get; set; } = true;
    public bool Fullscreen { get; set; }
    public bool ShowFinishers { get; set; } = true;
    public string ServerUrl { get; set; } = "http://localhost:5080";

    /// <summary>Keyboard bindings per local player: key name (Avalonia <c>Key</c> name) to button.</summary>
    public Dictionary<string, InputButtons>[] KeyBindings { get; set; } = [DefaultKeysPlayer1(), DefaultKeysPlayer2()];

    public static Dictionary<string, InputButtons> DefaultKeysPlayer1() => new(StringComparer.Ordinal)
    {
        ["W"] = InputButtons.Up,
        ["S"] = InputButtons.Down,
        ["A"] = InputButtons.Left,
        ["D"] = InputButtons.Right,
        ["J"] = InputButtons.Light,
        ["K"] = InputButtons.Heavy,
        ["L"] = InputButtons.Grab,
        ["U"] = InputButtons.Block,
        ["I"] = InputButtons.Special,
        ["O"] = InputButtons.Dodge,
        ["H"] = InputButtons.Tag,
        ["Space"] = InputButtons.Rage,
    };

    public static Dictionary<string, InputButtons> DefaultKeysPlayer2() => new(StringComparer.Ordinal)
    {
        ["Up"] = InputButtons.Up,
        ["Down"] = InputButtons.Down,
        ["Left"] = InputButtons.Left,
        ["Right"] = InputButtons.Right,
        ["NumPad1"] = InputButtons.Light,
        ["NumPad2"] = InputButtons.Heavy,
        ["NumPad3"] = InputButtons.Grab,
        ["NumPad4"] = InputButtons.Block,
        ["NumPad5"] = InputButtons.Special,
        ["NumPad6"] = InputButtons.Dodge,
        ["NumPad0"] = InputButtons.Tag,
        ["NumPad7"] = InputButtons.Rage,
    };
}
