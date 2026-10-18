using TarkamArena.Core.Combat;
using ThreeNet;

namespace TarkamArena.Engine.Input;

/// <summary>
/// Combines keyboard bindings and gamepads into one <see cref="InputFrame"/> per local player.
/// Keys arrive as names from the UI layer (Avalonia's <c>Key.ToString()</c>), so the engine does
/// not depend on a UI toolkit.
/// </summary>
public sealed class InputMapper : IDisposable
{
    private readonly HashSet<string> _down = new(StringComparer.Ordinal);
    private readonly Dictionary<string, InputButtons>[] _bindings;
    private readonly Gamepads? _gamepads;

    public InputMapper(Dictionary<string, InputButtons>[] bindings)
    {
        _bindings = bindings;
        try
        {
            _gamepads = new Gamepads { DeadZone = 0.3f };
        }
        catch (Exception e) when (e is ThreeNetException or DllNotFoundException or EntryPointNotFoundException)
        {
            _gamepads = null;
        }
    }

    public void KeyDown(string key) => _down.Add(key);

    public void KeyUp(string key) => _down.Remove(key);

    public void ReleaseAll() => _down.Clear();

    public bool IsDown(string key) => _down.Contains(key);

    public int ConnectedGamepads => _gamepads?.Connected.Count ?? 0;

    /// <summary>Call once per simulation tick before reading players.</summary>
    public void Poll() => _gamepads?.Update();

    /// <summary>Input for local player <paramref name="player"/> (0 or 1). Gamepad slot N maps to player N.</summary>
    public InputFrame Read(int player)
    {
        var buttons = InputButtons.None;
        if (player < _bindings.Length)
            foreach (var (key, button) in _bindings[player])
                if (_down.Contains(key)) buttons |= button;

        if (_gamepads is not null && player < _gamepads.SlotCount)
        {
            var pad = _gamepads.Get(player);
            if (pad.IsConnected) buttons |= FromPad(pad);
        }

        // Opposite directions cancel out (SOCD cleaning), as fighting games require.
        if ((buttons & (InputButtons.Left | InputButtons.Right)) == (InputButtons.Left | InputButtons.Right))
            buttons &= ~(InputButtons.Left | InputButtons.Right);
        if ((buttons & (InputButtons.Up | InputButtons.Down)) == (InputButtons.Up | InputButtons.Down))
            buttons &= ~InputButtons.Down;
        return new InputFrame(buttons);
    }

    private static InputButtons FromPad(GamepadState pad)
    {
        var b = InputButtons.None;
        if (pad.IsDown(GamepadButton.DPadUp) || pad.LeftStick.Y > 0.5f) b |= InputButtons.Up;
        if (pad.IsDown(GamepadButton.DPadDown) || pad.LeftStick.Y < -0.5f) b |= InputButtons.Down;
        if (pad.IsDown(GamepadButton.DPadLeft) || pad.LeftStick.X < -0.5f) b |= InputButtons.Left;
        if (pad.IsDown(GamepadButton.DPadRight) || pad.LeftStick.X > 0.5f) b |= InputButtons.Right;
        if (pad.IsDown(GamepadButton.West)) b |= InputButtons.Light;
        if (pad.IsDown(GamepadButton.North)) b |= InputButtons.Heavy;
        if (pad.IsDown(GamepadButton.East)) b |= InputButtons.Grab;
        if (pad.IsDown(GamepadButton.South)) b |= InputButtons.Special;
        if (pad.IsDown(GamepadButton.RightBumper) || pad.RightTrigger > 0.5f) b |= InputButtons.Block;
        if (pad.IsDown(GamepadButton.LeftBumper)) b |= InputButtons.Dodge;
        if (pad.LeftTrigger > 0.5f) b |= InputButtons.Tag;
        if (pad.IsDown(GamepadButton.LeftStick) || pad.IsDown(GamepadButton.RightStick)) b |= InputButtons.Rage;
        return b;
    }

    public void Rumble(int player, float strength, TimeSpan duration)
    {
        if (_gamepads is null) return;
        _gamepads.Rumble(player, strength, strength * 0.6f, duration);
    }

    public void Dispose() => _gamepads?.Dispose();
}
