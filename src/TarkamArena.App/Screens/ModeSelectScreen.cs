using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.AI;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;

namespace TarkamArena.App.Screens;

/// <summary>Picks the kind of local match and its options before character select.</summary>
public sealed class ModeSelectScreen : Screen
{
    private static readonly GameMode[] s_modes = [GameMode.Versus, GameMode.TwoVsTwo, GameMode.TagBattle, GameMode.FreeForAll, GameMode.Custom];
    private static readonly int[] s_timers = [30, 60, 99, 0];
    private static readonly int[] s_damage = [50, 75, 100, 150, 200];

    private GameMode _mode = GameMode.Versus;
    private bool _versusHuman;
    private CpuDifficulty _difficulty = CpuDifficulty.Normal;
    private int _rounds = 2;
    private int _timer = 99;
    private int _damagePercent = 100;
    private bool _events = true;
    private bool _finishers = true;
    private readonly TextBlock _description = Ui.Wrap("", "body").With(t => t.MaxWidth = 520);
    private readonly StackPanel _custom = new() { Spacing = 8 };
    private readonly List<Button> _modeButtons = [];

    public ModeSelectScreen()
    {
        var modes = new StackPanel { Spacing = 10 };
        foreach (var m in s_modes)
        {
            var b = Ui.Banner(GameModes.Name(m).ToString().ToUpperInvariant(), () => Start());
            b.GotFocus += (_, _) => Select(m);
            modes.Children.Add(b);
            _modeButtons.Add(b);
            Nav(b);
        }

        var opponent = new OptionRow(Loc.T("mode.opponent"), 2, 0, i => i == 0 ? Loc.T("common.cpu") : Loc.T("common.player", 2), i => _versusHuman = i == 1);
        var difficulty = new OptionRow(Loc.T("mode.difficulty"), 4, 1, i => CpuBrain.Name((CpuDifficulty)i).ToString(), i => _difficulty = (CpuDifficulty)i);
        Nav(opponent, difficulty);

        var rounds = new OptionRow(Loc.T("hud.round", "").Trim(), 5, 1, i => $"{i + 1}", i => _rounds = i + 1);
        var timer = new OptionRow(Loc.T("hud.timeUp"), s_timers.Length, 2, i => s_timers[i] == 0 ? "∞" : $"{s_timers[i]}s", i => _timer = s_timers[i]);
        var damage = new OptionRow("Damage", s_damage.Length, 2, i => $"{s_damage[i]}%", i => _damagePercent = s_damage[i]);
        var events = new OptionRow(Loc.T("stage.events"), 2, 0, i => i == 0 ? "ON" : "OFF", i => _events = i == 0);
        var finishers = new OptionRow(Loc.T("select.finishers"), 2, 0, i => i == 0 ? "ON" : "OFF", i => _finishers = i == 0);
        _custom.Children.AddRange([rounds, timer, damage, events, finishers]);
        Nav(rounds, timer, damage, events, finishers);

        var options = Ui.V(10, Ui.Text(Loc.T("settings.title").ToUpperInvariant(), "eyebrow"), opponent, difficulty, _custom);
        var right = Ui.Panel(Ui.V(20, _description, options), 28);
        right.VerticalAlignment = VerticalAlignment.Top;
        right.Width = 600;

        var body = Ui.Columns("Auto,*", Ui.V(14, new SignText(Loc.T("mode.title").ToUpperInvariant(), 52), modes), right.With(r => r.HorizontalAlignment = HorizontalAlignment.Right));
        body.Margin = new Thickness(80, 56, 80, 56);
        Content = body;
        Select(GameMode.Versus);
    }

    private void Select(GameMode mode)
    {
        _mode = mode;
        _description.Text = mode switch
        {
            GameMode.Versus => Loc.T("mode.versus.desc"),
            GameMode.TwoVsTwo => Loc.T("mode.twovtwo.desc"),
            GameMode.TagBattle => Loc.T("mode.tag.desc"),
            GameMode.FreeForAll => Loc.T("mode.ffa.desc"),
            _ => Loc.T("mode.custom.desc"),
        };
        _custom.IsVisible = mode == GameMode.Custom;
    }

    public override bool HandleAction(MenuAction action)
    {
        if (action is MenuAction.Left or MenuAction.Right && NavItems[NavIndex] is OptionRow row)
        {
            row.Step(action == MenuAction.Right ? 1 : -1);
            return true;
        }
        if (action is MenuAction.Left or MenuAction.Right) action = action == MenuAction.Left ? MenuAction.Up : MenuAction.Down;
        if (action == MenuAction.Confirm && NavItems[NavIndex] is OptionRow r)
        {
            r.Step(1);
            return true;
        }
        if (action is MenuAction.Up or MenuAction.Down)
        {
            // Skip hidden custom options.
            int dir = action == MenuAction.Up ? -1 : 1;
            do Move(dir); while (!NavItems[NavIndex].IsEffectivelyVisible);
            return true;
        }
        return base.HandleAction(action);
    }

    private void Start()
    {
        var mode = _mode == GameMode.Custom ? GameMode.Versus : _mode;
        var plan = MatchPlan.For(mode, _versusHuman);
        plan.Difficulty = _difficulty;
        if (_mode == GameMode.Custom)
        {
            plan.Rules = MatchRules.For(GameMode.Versus) with
            {
                Mode = GameMode.Custom,
                RoundsToWin = _rounds,
                RoundSeconds = _timer == 0 ? 99 : _timer,
                TimerEnabled = _timer != 0,
                DamagePercent = _damagePercent,
                ArenaEventsEnabled = _events,
                FinishersEnabled = _finishers,
            };
        }
        plan.Rules = plan.Rules with { FinishersEnabled = plan.Rules.FinishersEnabled && Game.Settings.ShowFinishers };
        Window.Show(new CharacterSelectScreen(plan));
    }
}
