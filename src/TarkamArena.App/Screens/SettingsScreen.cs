using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Settings;

namespace TarkamArena.App.Screens;

/// <summary>Language, audio, graphics and the control reference.</summary>
public sealed class SettingsScreen : Screen
{
    public SettingsScreen() => Build();

    private void Build()
    {
        NavItems.Clear();
        var s = Game.Settings;
        string Pct(int i) => $"{i * 10}%";
        string OnOff(int i) => i == 0 ? "ON" : "OFF";
        var rows = new List<OptionRow>
        {
            new(Loc.T("settings.language"), 2, s.Language == Language.Indonesian ? 0 : 1, i => i == 0 ? "Bahasa Indonesia" : "English", i =>
            {
                s.Language = i == 0 ? Language.Indonesian : Language.English;
                Loc.SetLanguage(s.Language);
                Game.Save();
                Build();
            }),
            new(Loc.T("settings.master"), 11, (int)Math.Round(s.MasterVolume * 10), Pct, i => { s.MasterVolume = i / 10f; Game.ApplyAudioSettings(); }),
            new(Loc.T("settings.music"), 11, (int)Math.Round(s.MusicVolume * 10), Pct, i => { s.MusicVolume = i / 10f; Game.ApplyAudioSettings(); }),
            new(Loc.T("settings.sfx"), 11, (int)Math.Round(s.SfxVolume * 10), Pct, i => { s.SfxVolume = i / 10f; Game.ApplyAudioSettings(); Game.Audio.Play(Engine.Audio.Sfx.HitLight); }),
            new(Loc.T("settings.quality"), 4, (int)s.Quality, i => Loc.T($"settings.quality.{((GraphicsQuality)i).ToString().ToLowerInvariant()}"), i => s.Quality = (GraphicsQuality)i),
            new(Loc.T("settings.motionBlur"), 2, s.MotionBlur ? 0 : 1, OnOff, i => s.MotionBlur = i == 0),
            new(Loc.T("settings.weather"), 2, s.DynamicWeather ? 0 : 1, OnOff, i => s.DynamicWeather = i == 0),
            new(Loc.T("settings.finishers"), 2, s.ShowFinishers ? 0 : 1, OnOff, i => s.ShowFinishers = i == 0),
            new(Loc.T("settings.fullscreen"), 2, s.Fullscreen ? 0 : 1, OnOff, i => { if ((i == 0) != Window.WindowState.Equals(WindowState.FullScreen)) Window.ToggleFullscreen(); }),
        };
        foreach (var r in rows) Nav(r);

        var controls = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("260,160,160"), RowSpacing = 4 };
        (string Key, InputButtons Button)[] actions =
        [
            ("controls.move", InputButtons.Left), ("controls.jump", InputButtons.Up), ("controls.crouch", InputButtons.Down),
            ("controls.light", InputButtons.Light), ("controls.heavy", InputButtons.Heavy), ("controls.grab", InputButtons.Grab),
            ("controls.block", InputButtons.Block), ("controls.special", InputButtons.Special), ("controls.dodge", InputButtons.Dodge),
            ("controls.tag", InputButtons.Tag), ("controls.rage", InputButtons.Rage),
        ];
        string KeyFor(int player, InputButtons b) => s.KeyBindings[player].Where(kv => kv.Value == b).Select(kv => kv.Key).FirstOrDefault() ?? "-";
        int row = 0;
        void Add(Control c, int col)
        {
            Grid.SetRow(c, row);
            Grid.SetColumn(c, col);
            controls.Children.Add(c);
        }
        controls.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        Add(Ui.Text(Loc.T("settings.controls").ToUpperInvariant(), "eyebrow"), 0);
        Add(Ui.Text(Loc.T("common.player", 1).ToUpperInvariant(), "eyebrow"), 1);
        Add(Ui.Text(Loc.T("common.player", 2).ToUpperInvariant(), "eyebrow"), 2);
        foreach (var (key, button) in actions)
        {
            row++;
            controls.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Add(Ui.Text(Loc.T(key), "body"), 0);
            string p1 = button == InputButtons.Left ? $"{KeyFor(0, InputButtons.Left)} / {KeyFor(0, InputButtons.Right)}" : KeyFor(0, button);
            string p2 = button == InputButtons.Left ? $"{KeyFor(1, InputButtons.Left)} / {KeyFor(1, InputButtons.Right)}" : KeyFor(1, button);
            Add(Ui.Text(p1, "mono"), 1);
            Add(Ui.Text(p2, "mono"), 2);
        }
        row++;
        controls.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        Add(Ui.Wrap("Gamepad: X/□ light · Y/△ heavy · B/○ grab · A/✕ special · RB block · LB dodge · LT tag · L3 rage", "body", "muted").With(t => Grid.SetColumnSpan(t, 3)), 0);

        var left = Ui.V(10, new SignText(Loc.T("settings.title").ToUpperInvariant(), 52));
        left.Children.AddRange(rows);
        var root = Ui.Columns("*,Auto", left, Ui.Panel(controls, 24).With(p => p.VerticalAlignment = VerticalAlignment.Top));
        root.Margin = new Thickness(80, 44, 80, 40);
        Content = root;
        NavIndex = 0;
        FocusCurrent();
    }

    public override bool HandleAction(MenuAction action)
    {
        if (action is MenuAction.Left or MenuAction.Right && NavItems[NavIndex] is OptionRow row)
        {
            row.Step(action == MenuAction.Right ? 1 : -1);
            return true;
        }
        if (action == MenuAction.Confirm && NavItems[NavIndex] is OptionRow r)
        {
            r.Step(1);
            return true;
        }
        return base.HandleAction(action);
    }

    public override void OnLeave() => Game.Save();
}
