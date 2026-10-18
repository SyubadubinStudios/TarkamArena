using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TarkamArena.App.Controls;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Match;
using TarkamArena.Core.Localization;

namespace TarkamArena.App.Screens;

/// <summary>
/// The fight HUD, drawn by Avalonia over the 3D view: life bars with damage trails, round pips,
/// rage and adrenaline meters, timer, crowd VU meter, callouts and the finisher list.
/// Updated every rendered frame without allocating.
/// </summary>
public sealed class FightHud : Grid
{
    private sealed class FighterPanel
    {
        public required Border Root;
        public required LifeBar Life;
        public required SegmentMeter Rage;
        public required SegmentMeter Adrenaline;
        public required TextBlock Name;
        public required TextBlock Status;
        public required StackPanel Pips;
        public required TextBlock Combo;
        public double ComboTimer;
        public int LastWins = -1;
    }

    private readonly MatchContext _context;
    private readonly FighterPanel[] _panels;
    private readonly TextBlock _timer;
    private readonly CrowdMeter _crowd = new() { Width = 360, Height = 44 };
    private readonly SignText _callout = new("", 104);
    private readonly TextBlock _subCallout = Ui.Text("", "h2");
    private readonly Border _calloutBox;
    private readonly TextBlock _eventToast = Ui.Text("", "h3");
    private readonly Border _eventBox;
    private readonly Border _finisherPanel;
    private readonly TextBlock _finisherList = Ui.Wrap("", "mono").With(t => { t.FontSize = 17; t.LineHeight = 26; });
    private readonly int _localSlot;
    private double _calloutTimer, _eventTimer;
    private int _lastTimer = -1;

    public FightHud(MatchContext context, int localSlot)
    {
        _context = context;
        _localSlot = localSlot;
        IsHitTestVisible = false;
        int n = context.Characters.Length;
        bool ffa = context.Rules.Mode == GameMode.FreeForAll;
        _panels = new FighterPanel[n];

        var leftColumn = new StackPanel { Spacing = 10, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(40, 28, 0, 0) };
        var rightColumn = new StackPanel { Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 28, 40, 0) };
        var ffaRow = new UniformGridLike(n);

        for (int i = 0; i < n; i++)
        {
            byte team = GameModes.TeamOf(context.Rules.Mode, i);
            bool mirrored = !ffa && team == 1;
            bool compact = ffa || (context.Rules.Mode is GameMode.TwoVsTwo or GameMode.TagBattle && i % 2 == 1);
            var panel = CreatePanel(i, mirrored, compact);
            _panels[i] = panel;
            if (ffa) ffaRow.Add(panel.Root);
            else if (team == 0) leftColumn.Children.Add(panel.Root);
            else rightColumn.Children.Add(panel.Root);
        }

        if (ffa)
        {
            ffaRow.Panel.Margin = new Thickness(40, 24, 40, 0);
            Children.Add(ffaRow.Panel);
        }
        else
        {
            Children.Add(leftColumn);
            Children.Add(rightColumn);
        }

        _timer = new TextBlock { FontFamily = Ui.Display, FontSize = 52, Foreground = Ui.Arang, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var timerPlate = new Border
        {
            Width = 104,
            Height = 92,
            Background = Ui.Spanduk,
            BorderBrush = Ui.Arang,
            BorderThickness = new Thickness(4),
            Child = _timer,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, ffa ? 110 : 22, 0, 0),
            RenderTransform = new SkewTransform(-6, 0),
            IsVisible = context.Rules.TimerEnabled,
        };
        Children.Add(timerPlate);

        var crowdBox = Ui.V(4, _crowd, Ui.Text(Loc.T("hud.crowd").ToUpperInvariant(), "eyebrow").With(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.FontSize = 13; }));
        crowdBox.HorizontalAlignment = HorizontalAlignment.Center;
        crowdBox.VerticalAlignment = VerticalAlignment.Bottom;
        crowdBox.Margin = new Thickness(0, 0, 0, 22);
        Children.Add(crowdBox);

        _calloutBox = new Border
        {
            Child = Ui.V(6, _callout.With(c => c.HorizontalAlignment = HorizontalAlignment.Center), _subCallout.With(t => t.HorizontalAlignment = HorizontalAlignment.Center)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 80),
            Opacity = 0,
        };
        Children.Add(_calloutBox);

        _eventBox = new Border
        {
            Child = Ui.H(10, Ui.Text("!", "h3").With(t => t.Foreground = Ui.Cabai), _eventToast),
            Background = Ui.PanelFill,
            BorderBrush = Ui.Cabai,
            BorderThickness = new Thickness(0, 0, 0, 3),
            Padding = new Thickness(16, 8),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, ffa ? 220 : 130, 0, 0),
            Opacity = 0,
        };
        Children.Add(_eventBox);

        _finisherPanel = new Border
        {
            Child = Ui.V(8, Ui.Text(Loc.T("hud.finishThem").ToUpperInvariant(), "eyebrow"), _finisherList),
            Background = Ui.PanelFill,
            BorderBrush = Ui.Spanduk,
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(18, 12),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(40, 0, 0, 110),
            IsVisible = false,
        };
        Children.Add(_finisherPanel);
    }

    /// <summary>Lays panels out evenly in a row for free-for-all.</summary>
    private sealed class UniformGridLike(int count)
    {
        public Grid Panel { get; } = new() { ColumnDefinitions = ColumnDefinitions.Parse(string.Join(",", Enumerable.Repeat("*", count))), ColumnSpacing = 24 };
        private int _next;

        public void Add(Control c)
        {
            Grid.SetColumn(c, _next++);
            Panel.Children.Add(c);
        }
    }

    private FighterPanel CreatePanel(int slot, bool mirrored, bool compact)
    {
        var c = _context.Characters[slot];
        double width = compact ? 380 : 640;
        var life = new LifeBar { Width = width, Height = compact ? 20 : 34, Mirrored = mirrored, Fill = Ui.Spanduk };
        var rage = new SegmentMeter { Width = compact ? 150 : 220, Height = 10, Mirrored = mirrored, Fill = Ui.Cabai };
        var adrenaline = new SegmentMeter { Width = compact ? 110 : 160, Height = 10, Mirrored = mirrored, Fill = Ui.Daun };
        var name = new TextBlock { Text = c.Name.ToString().ToUpperInvariant(), FontFamily = Ui.Display, FontSize = compact ? 18 : 26, Foreground = Ui.Kapur };
        var status = Ui.Text("", "eyebrow").With(t => t.FontSize = 13);
        var pips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        var combo = new TextBlock { FontFamily = Ui.Display, FontSize = compact ? 22 : 34, Foreground = Ui.Spanduk, Opacity = 0 };

        var nameRow = mirrored ? Ui.H(12, pips, status, name) : Ui.H(12, name, status, pips);
        var meters = mirrored ? Ui.H(10, adrenaline, rage) : Ui.H(10, rage, adrenaline);
        var stack = Ui.V(6, nameRow, life, meters, combo);
        if (mirrored)
        {
            nameRow.HorizontalAlignment = HorizontalAlignment.Right;
            meters.HorizontalAlignment = HorizontalAlignment.Right;
            combo.HorizontalAlignment = HorizontalAlignment.Right;
        }
        return new FighterPanel
        {
            Root = new Border { Child = stack },
            Life = life,
            Rage = rage,
            Adrenaline = adrenaline,
            Name = name,
            Status = status,
            Pips = pips,
            Combo = combo,
        };
    }

    public void Update(in MatchState state, double dt)
    {
        for (int i = 0; i < _panels.Length; i++)
        {
            ref readonly var f = ref state.Fighters[i];
            var p = _panels[i];
            p.Root.Opacity = f.OnField || _context.Rules.Mode != GameMode.TagBattle ? 1 : 0.55;
            p.Life.Set(f.MaxHealth > 0 ? f.Health / (double)f.MaxHealth : 0, dt);
            p.Rage.Active = f.RageActive;
            p.Rage.Set(f.RageActive ? f.RageTimer / (double)MatchSimulation.RageDuration : f.Rage / (double)MatchSimulation.MeterMax);
            p.Adrenaline.Active = f.AdrenalineActive;
            p.Adrenaline.Set(f.AdrenalineActive ? f.AdrenalineTimer / (double)MatchSimulation.AdrenalineDuration : f.Adrenaline / (double)MatchSimulation.MeterMax);
            string status = f.RageActive ? Loc.T("hud.rage") : f.Rage >= MatchSimulation.MeterMax ? Loc.T("hud.rageReady") : f.AdrenalineActive ? Loc.T("hud.adrenaline") : "";
            if (p.Status.Text != status) p.Status.Text = status.ToUpperInvariant();

            int wins = state.RoundWins[f.Team];
            if (wins != p.LastWins)
            {
                p.LastWins = wins;
                p.Pips.Children.Clear();
                for (int r = 0; r < Math.Min(5, _context.Rules.RoundsToWin); r++)
                    p.Pips.Children.Add(new Border { Width = 16, Height = 16, BorderBrush = Ui.Arang, BorderThickness = new Thickness(2), Background = r < wins ? Ui.Cabai : Ui.Line, RenderTransform = new RotateTransform(45) });
            }

            if (p.ComboTimer > 0)
            {
                p.ComboTimer -= dt;
                p.Combo.Opacity = Math.Clamp(p.ComboTimer * 2, 0, 1);
            }
        }

        int seconds = state.TimerSeconds;
        if (seconds != _lastTimer)
        {
            _lastTimer = seconds;
            _timer.Text = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _timer.Foreground = seconds <= 10 ? Ui.Cabai : Ui.Arang;
        }
        _crowd.Set(state.Crowd / (double)MatchSimulation.CrowdMax, dt);

        if (_calloutTimer > 0)
        {
            _calloutTimer -= dt;
            _calloutBox.Opacity = Math.Clamp(_calloutTimer * 3, 0, 1);
        }
        if (_eventTimer > 0)
        {
            _eventTimer -= dt;
            _eventBox.Opacity = Math.Clamp(_eventTimer * 2, 0, 1);
        }

        bool showFinishers = state.Phase == MatchPhase.FinisherWindow && state.FinisherExecutor == _localSlot && _localSlot >= 0;
        if (showFinishers && !_finisherPanel.IsVisible)
        {
            bool legendaryLocked = state.Crowd < FinisherDefinition.LegendaryCrowdRequirement;
            var lines = _context.Finishers[_localSlot].Select(d =>
                $"{d.SequenceGlyphs,-11}  {FinisherFactory.KindName(d.Kind)} · {d.Name}{(d.Kind == FinisherKind.Legendary && legendaryLocked ? "  ✕" : "")}");
            _finisherList.Text = string.Join("\n", lines);
        }
        _finisherPanel.IsVisible = showFinishers;
    }

    public void Callout(string text, string sub = "", double seconds = 1.4, IBrush? ink = null)
    {
        _callout.Text = text.ToUpperInvariant();
        _subCallout.Text = sub;
        _calloutTimer = seconds;
        _calloutBox.Opacity = 1;
    }

    public void OnEvent(in MatchEvent e, in MatchState state)
    {
        switch (e.Kind)
        {
            case MatchEventKind.RoundStart:
                bool final = false;
                for (int i = 0; i < state.FighterCount; i++)
                    if (_context.Rules.RoundsToWin > 1 && state.RoundWins[state.Fighters[i].Team] == _context.Rules.RoundsToWin - 1) final = true;
                Callout(final ? Loc.T("hud.finalRound") : Loc.T("hud.round", e.Value), "", 1.8);
                break;
            case MatchEventKind.Fight: Callout(Loc.T("hud.fight"), "", 1.0); break;
            case MatchEventKind.KO: Callout(Loc.T("hud.ko"), "", 1.6); break;
            case MatchEventKind.TimeUp: Callout(Loc.T("hud.timeUp"), "", 1.6); break;
            case MatchEventKind.FinisherPrompt: Callout(Loc.T("hud.finishThem"), "", 2.2); break;
            case MatchEventKind.FinisherStart:
                var def = _context.Finishers[e.Slot][e.Value];
                Callout(FinisherFactory.KindName(def.Kind).ToString(), def.Name.ToString(), 3.5);
                break;
            case MatchEventKind.Mercy: Callout(Loc.T("hud.mercy"), "", 2.4); break;
            case MatchEventKind.MatchWin:
            case MatchEventKind.RoundWin when e.Value < 0:
                if (e.Value < 0) Callout(Loc.T("hud.draw"), "", 2);
                else if (e.Kind == MatchEventKind.MatchWin)
                {
                    string name = WinnerName(state, e.Value);
                    Callout(Loc.T("hud.wins", name), "", 3);
                }
                break;
            case MatchEventKind.Combo when e.Target >= 0 && e.Slot >= 0:
                ShowSide(e.Slot, Loc.T("hud.combo", e.Value));
                break;
            case MatchEventKind.CounterHit when e.Slot >= 0: ShowSide(e.Slot, Loc.T("hud.counter")); break;
            case MatchEventKind.Parry when e.Slot >= 0: ShowSide(e.Slot, Loc.T("hud.parry")); break;
            case MatchEventKind.TeamCombo when e.Slot >= 0: ShowSide(e.Slot, Loc.T("hud.teamCombo")); break;
            case MatchEventKind.RageActivated when e.Slot >= 0: ShowSide(e.Slot, Loc.T("hud.rage")); break;
            case MatchEventKind.ArenaEvent:
                _eventToast.Text = _context.Stage.Events[e.Value].Name.ToString();
                _eventTimer = 2.5;
                _eventBox.Opacity = 1;
                break;
        }
    }

    private string WinnerName(in MatchState state, int team)
    {
        for (int i = 0; i < state.FighterCount; i++)
            if (state.Fighters[i].Team == team) return _context.Characters[i].Name.ToString();
        return "";
    }

    private void ShowSide(int slot, string text)
    {
        if (slot >= _panels.Length) return;
        var p = _panels[slot];
        p.Combo.Text = text.ToUpperInvariant();
        p.ComboTimer = 1.6;
        p.Combo.Opacity = 1;
    }
}
