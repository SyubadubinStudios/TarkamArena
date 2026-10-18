using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;

namespace TarkamArena.App.Screens;

/// <summary>
/// The fight HUD drawn by Avalonia over the 3D view: portrait plates and life bars with damage
/// trails, round pips, rage and adrenaline meters, the timer plate, the crowd VU meter, callouts
/// stamped on a brush stroke, impact flashes, speed lines, letterbox bars for cinematics, a
/// low-health vignette and confetti. Updated every rendered frame without allocating.
/// </summary>
public sealed class FightHud : Grid
{
    private sealed class FighterPanel
    {
        public required Border Root;
        public required LifeBar Life;
        public required SegmentMeter Rage;
        public required SegmentMeter Adrenaline;
        public required TextBlock Status;
        public required StackPanel Pips;
        public required TextBlock Combo;
        public required Border Plate;
        public double ComboTimer;
        public int LastWins = -1;
        public int LastHealth = -1;
        public int LastCombo;
    }

    private readonly MatchContext _context;
    private readonly FighterPanel[] _panels;
    private readonly TextBlock _timer;
    private readonly Border _timerPlate;
    private readonly CrowdMeter _crowd = new() { Width = 360, Height = 44 };
    private readonly SignText _callout = new("", 104);
    private readonly TextBlock _subCallout = Ui.Text("", "h2");
    private readonly Grid _calloutBox;
    private readonly Image _brush;
    private readonly TextBlock _eventToast = Ui.Text("", "h3");
    private readonly Border _eventBox;
    private readonly Border _finisherPanel;
    private readonly TextBlock _finisherList = Ui.Wrap("", "mono").With(t => { t.FontSize = 17; t.LineHeight = 26; });
    private readonly Border _flash = new() { Background = Brushes.White, Opacity = 0, IsHitTestVisible = false };
    private readonly Border _vignette;
    private readonly Border _letterTop = new() { Background = Brushes.Black, Height = 90, VerticalAlignment = VerticalAlignment.Top, IsHitTestVisible = false };
    private readonly Border _letterBottom = new() { Background = Brushes.Black, Height = 90, VerticalAlignment = VerticalAlignment.Bottom, IsHitTestVisible = false };
    private readonly ParticleField _speedLines = new(ParticleStyle.SpeedLines, 70) { Intensity = 0 };
    private readonly ParticleField _confetti = new(ParticleStyle.Confetti, 140) { Intensity = 0 };
    private readonly Control _topLayer;
    private readonly int _localSlot;
    private double _calloutTimer, _eventTimer, _speedTimer, _confettiTimer;
    private int _lastTimer = -1;
    private bool _letterbox;
    private double _vignetteTime;

    public FightHud(MatchContext context, int localSlot)
    {
        _context = context;
        _localSlot = localSlot;
        IsHitTestVisible = false;
        ClipToBounds = true;
        int n = context.Characters.Length;
        bool ffa = context.Rules.Mode == GameMode.FreeForAll;
        _panels = new FighterPanel[n];

        // Low-health vignette: a red glow creeping in from the screen edges.
        _vignette = new Border
        {
            IsHitTestVisible = false,
            Opacity = 0,
            Background = new RadialGradientBrush
            {
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 0xE0, 0x32, 0x2B), 0.55),
                    new GradientStop(Color.FromArgb(0xB0, 0xE0, 0x32, 0x2B), 1),
                },
            },
        };
        Children.Add(_vignette);
        Children.Add(_speedLines);
        TranslateTransform Slide(double y) => new(0, y)
        {
            Transitions = [new Avalonia.Animation.DoubleTransition { Property = TranslateTransform.YProperty, Duration = TimeSpan.FromMilliseconds(380), Easing = new Avalonia.Animation.Easings.CubicEaseOut() }],
        };
        _letterTop.RenderTransform = Slide(-90);
        _letterBottom.RenderTransform = Slide(90);
        Children.Add(_letterTop);
        Children.Add(_letterBottom);

        var leftColumn = new StackPanel { Spacing = 10, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(36, 24, 0, 0) };
        var rightColumn = new StackPanel { Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 24, 36, 0) };
        var ffaRow = new Grid { ColumnDefinitions = ColumnDefinitions.Parse(string.Join(",", Enumerable.Repeat("*", n))), ColumnSpacing = 24, Margin = new Thickness(36, 22, 36, 0) };

        for (int i = 0; i < n; i++)
        {
            byte team = GameModes.TeamOf(context.Rules.Mode, i);
            bool mirrored = !ffa && team == 1;
            bool compact = ffa || (context.Rules.Mode is GameMode.TwoVsTwo or GameMode.TagBattle && i % 2 == 1);
            var panel = CreatePanel(i, mirrored, compact);
            _panels[i] = panel;
            if (ffa)
            {
                Grid.SetColumn(panel.Root, i);
                ffaRow.Children.Add(panel.Root);
            }
            else if (team == 0) leftColumn.Children.Add(panel.Root);
            else rightColumn.Children.Add(panel.Root);
        }
        var top = new Grid();
        if (ffa) top.Children.Add(ffaRow);
        else
        {
            top.Children.Add(leftColumn);
            top.Children.Add(rightColumn);
        }
        _topLayer = top;
        Children.Add(top);

        _timer = new TextBlock { FontFamily = Ui.Display, FontSize = 52, Foreground = Ui.Arang, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _timerPlate = new Border
        {
            Width = 104,
            Height = 92,
            Background = Ui.Spanduk,
            BorderBrush = Ui.Arang,
            BorderThickness = new Thickness(4),
            Child = _timer,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, ffa ? 110 : 18, 0, 0),
            RenderTransform = new SkewTransform(-6, 0),
            IsVisible = context.Rules.TimerEnabled,
        };
        Children.Add(_timerPlate);

        var crowdBox = Ui.V(4, _crowd, Ui.Text(Loc.T("hud.crowd").ToUpperInvariant(), "eyebrow").With(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.FontSize = 13; }));
        crowdBox.HorizontalAlignment = HorizontalAlignment.Center;
        crowdBox.VerticalAlignment = VerticalAlignment.Bottom;
        crowdBox.Margin = new Thickness(0, 0, 0, 22);
        Children.Add(crowdBox);

        // Callouts are stamped on a red ink brush stroke (Rodin 2D art).
        _brush = Ui.Picture(Images.Load("ui/brush-stroke.png", 1600), 1100, 270, Stretch.Fill);
        _calloutBox = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 80), Opacity = 0 };
        _calloutBox.Children.Add(_brush.With(b => { b.HorizontalAlignment = HorizontalAlignment.Center; b.VerticalAlignment = VerticalAlignment.Center; b.Opacity = 0.92; }));
        _calloutBox.Children.Add(Ui.V(4, _callout.With(c => c.HorizontalAlignment = HorizontalAlignment.Center), _subCallout.With(t => t.HorizontalAlignment = HorizontalAlignment.Center))
            .With(v => v.VerticalAlignment = VerticalAlignment.Center));
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
        Children.Add(_confetti);
        Children.Add(_flash);
    }

    private FighterPanel CreatePanel(int slot, bool mirrored, bool compact)
    {
        var c = _context.Characters[slot];
        double width = compact ? 340 : 580;
        var life = new LifeBar { Width = width, Height = compact ? 20 : 34, Mirrored = mirrored, Fill = Ui.Spanduk };
        var rage = new SegmentMeter { Width = compact ? 150 : 220, Height = 10, Mirrored = mirrored, Fill = Ui.Cabai };
        var adrenaline = new SegmentMeter { Width = compact ? 110 : 160, Height = 10, Mirrored = mirrored, Fill = Ui.Daun };
        var name = new TextBlock { Text = c.Name.ToString().ToUpperInvariant(), FontFamily = Ui.Display, FontSize = compact ? 18 : 26, Foreground = Ui.Kapur };
        var status = Ui.Text("", "eyebrow").With(t => t.FontSize = 13);
        var pips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        var combo = new TextBlock { FontFamily = Ui.Display, FontSize = compact ? 22 : 34, Foreground = Ui.Spanduk, Opacity = 0 };

        // Portrait plate: the fighter's head cropped from the Blender render, in a slanted frame.
        double plateSize = compact ? 54 : 78;
        var face = new Image
        {
            Source = Images.Portrait(c, 256),
            Width = plateSize * 2.1,
            Height = plateSize * 2.6,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -plateSize * 0.08, 0, 0),
            RenderTransform = mirrored ? new ScaleTransform(-1, 1) : null,
        };
        var plate = new Border
        {
            Width = plateSize,
            Height = plateSize,
            ClipToBounds = true,
            Background = mirrored ? Ui.Cabai : Ui.Terpal,
            BorderBrush = Ui.Spanduk,
            BorderThickness = new Thickness(3),
            Child = face,
            VerticalAlignment = VerticalAlignment.Top,
        };

        var nameRow = mirrored ? Ui.H(12, pips, status, name) : Ui.H(12, name, status, pips);
        var meters = mirrored ? Ui.H(10, adrenaline, rage) : Ui.H(10, rage, adrenaline);
        var stack = Ui.V(6, nameRow, life, meters, combo);
        if (mirrored)
        {
            nameRow.HorizontalAlignment = HorizontalAlignment.Right;
            meters.HorizontalAlignment = HorizontalAlignment.Right;
            combo.HorizontalAlignment = HorizontalAlignment.Right;
        }
        var row = mirrored ? Ui.H(12, stack, plate) : Ui.H(12, plate, stack);
        return new FighterPanel
        {
            Root = new Border { Child = row },
            Life = life,
            Rage = rage,
            Adrenaline = adrenaline,
            Status = status,
            Pips = pips,
            Combo = combo,
            Plate = plate,
        };
    }

    /// <summary>Slides the panels and timer in at the start of a match.</summary>
    public void Intro()
    {
        for (int i = 0; i < _panels.Length; i++)
        {
            bool right = _panels[i].Life.Mirrored;
            _ = Anim.SlideIn(_panels[i].Root, right ? 220 : -220, 120 + i * 80, 480);
        }
        _ = Anim.SlideIn(_timerPlate, 0, 300, 420, "skewX(-6deg)", -140);
    }

    public void Update(in MatchState state, double dt)
    {
        for (int i = 0; i < _panels.Length; i++)
        {
            ref readonly var f = ref state.Fighters[i];
            var p = _panels[i];
            p.Root.Opacity = f.OnField || _context.Rules.Mode != GameMode.TagBattle ? 1 : 0.55;
            p.Life.Set(f.MaxHealth > 0 ? f.Health / (double)f.MaxHealth : 0, dt);
            if (p.LastHealth >= 0 && f.Health < p.LastHealth - 40)
            {
                _ = Anim.Shake(p.Root, Math.Min(16, (p.LastHealth - f.Health) / 8.0));
                _ = Anim.Pop(p.Plate, 1.15, 180);
            }
            p.LastHealth = f.Health;
            p.Rage.Active = f.RageActive;
            p.Rage.Set(f.RageActive ? f.RageTimer / (double)MatchSimulation.RageDuration : f.Rage / (double)MatchSimulation.MeterMax);
            p.Adrenaline.Active = f.AdrenalineActive;
            p.Adrenaline.Set(f.AdrenalineActive ? f.AdrenalineTimer / (double)MatchSimulation.AdrenalineDuration : f.Adrenaline / (double)MatchSimulation.MeterMax);
            string status = f.RageActive ? Loc.T("hud.rage") : f.Rage >= MatchSimulation.MeterMax ? Loc.T("hud.rageReady") : f.AdrenalineActive ? Loc.T("hud.adrenaline") : "";
            if (p.Status.Text != status.ToUpperInvariant()) p.Status.Text = status.ToUpperInvariant();

            int wins = state.RoundWins[f.Team];
            if (wins != p.LastWins)
            {
                bool gained = p.LastWins >= 0 && wins > p.LastWins;
                p.LastWins = wins;
                p.Pips.Children.Clear();
                for (int r = 0; r < Math.Min(5, _context.Rules.RoundsToWin); r++)
                    p.Pips.Children.Add(new Border { Width = 16, Height = 16, BorderBrush = Ui.Arang, BorderThickness = new Thickness(2), Background = r < wins ? Ui.Cabai : Ui.Line, RenderTransform = new RotateTransform(45) });
                if (gained && wins > 0) _ = Anim.Stamp(p.Pips.Children[wins - 1], 0, 360, 3);
            }

            // Combo counter: pops on every extra hit while the combo is alive.
            int comboOnOthers = 0;
            for (int j = 0; j < state.FighterCount; j++)
                if (state.Fighters[j].LastAttacker == i && state.Fighters[j].ComboHits > comboOnOthers) comboOnOthers = state.Fighters[j].ComboHits;
            if (comboOnOthers >= 2 && comboOnOthers != p.LastCombo)
            {
                p.Combo.Text = Loc.T("hud.combo", comboOnOthers).ToUpperInvariant();
                p.ComboTimer = 1.4;
                p.Combo.Opacity = 1;
                _ = Anim.Pop(p.Combo, 1.4, 160);
            }
            p.LastCombo = comboOnOthers;

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
            if (seconds <= 10 && state.Phase == MatchPhase.Fight) _ = Anim.Pop(_timerPlate, 1.12, 200);
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
        if (_speedTimer > 0)
        {
            _speedTimer -= dt;
            _speedLines.Intensity = Math.Clamp(_speedTimer * 2, 0, 1);
        }
        if (_confettiTimer > 0)
        {
            _confettiTimer -= dt;
            _confetti.Intensity = _confettiTimer > 0 ? 1 : 0;
        }

        // Cinematic letterbox for finishers and the end of the match; the HUD steps aside.
        bool letterbox = state.Phase is MatchPhase.FinisherPerforming or MatchPhase.MatchOver;
        if (letterbox != _letterbox)
        {
            _letterbox = letterbox;
            ((TranslateTransform)_letterTop.RenderTransform!).Y = letterbox ? 0 : -90;
            ((TranslateTransform)_letterBottom.RenderTransform!).Y = letterbox ? 0 : 90;
            _topLayer.Opacity = letterbox ? 0.15 : 1;
        }

        // Low health: the screen edges glow red and breathe.
        if (_localSlot >= 0 && _localSlot < state.FighterCount)
        {
            ref readonly var me = ref state.Fighters[_localSlot];
            bool danger = state.Phase == MatchPhase.Fight && me.IsAlive && me.Health * 4 < me.MaxHealth;
            _vignetteTime += dt;
            _vignette.Opacity = danger ? 0.3 + 0.5 * (0.5 + 0.5 * Math.Sin(_vignetteTime * 8)) : 0;
        }

        bool showFinishers = state.Phase == MatchPhase.FinisherWindow && state.FinisherExecutor == _localSlot && _localSlot >= 0;
        if (showFinishers && !_finisherPanel.IsVisible)
        {
            bool legendaryLocked = state.Crowd < FinisherDefinition.LegendaryCrowdRequirement;
            var lines = _context.Finishers[_localSlot].Select(d =>
                $"{d.SequenceGlyphs,-11}  {FinisherFactory.KindName(d.Kind)} · {d.Name}{(d.Kind == FinisherKind.Legendary && legendaryLocked ? "  ✕" : "")}");
            _finisherList.Text = string.Join("\n", lines);
            _ = Anim.SlideIn(_finisherPanel, -300, 0, 380);
        }
        _finisherPanel.IsVisible = showFinishers;
    }

    public void Callout(string text, string sub = "", double seconds = 1.4, bool brush = true)
    {
        _callout.Text = text.ToUpperInvariant();
        _subCallout.Text = sub;
        _calloutTimer = seconds;
        _calloutBox.Opacity = 1;
        _brush.IsVisible = brush;
        _ = Anim.Stamp(_callout, 0, 360, 2.2, -6);
        if (brush) _ = Anim.SlideIn(_brush, -500, 0, 260);
    }

    private void Flash(Color color, double peak)
    {
        _flash.Background = new SolidColorBrush(color);
        _ = Anim.Flash(_flash, peak, 280);
    }

    private void SpeedLines(double seconds)
    {
        _speedTimer = seconds;
        _speedLines.Intensity = 1;
    }

    private void Confetti(double seconds)
    {
        _confettiTimer = seconds;
        _confetti.Intensity = 1;
        _confetti.Burst(seconds);
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
                if (e.Value == 1) Intro();
                break;
            case MatchEventKind.Fight:
                Callout(Loc.T("hud.fight"), "", 1.0);
                SpeedLines(0.7);
                Flash(Colors.White, 0.35);
                break;
            case MatchEventKind.KO:
                Callout(Loc.T("hud.ko"), "", 1.6);
                Flash(Colors.White, 0.9);
                SpeedLines(0.6);
                break;
            case MatchEventKind.CounterHit when e.Slot >= 0:
                ShowSide(e.Slot, Loc.T("hud.counter"));
                Flash(Color.FromRgb(0xF6, 0xC2, 0x1B), 0.22);
                break;
            case MatchEventKind.Parry when e.Slot >= 0:
                ShowSide(e.Slot, Loc.T("hud.parry"));
                Flash(Color.FromRgb(0x6B, 0xD0, 0xFF), 0.25);
                break;
            case MatchEventKind.TimeUp: Callout(Loc.T("hud.timeUp"), "", 1.6); break;
            case MatchEventKind.FinisherPrompt:
                Callout(Loc.T("hud.finishThem"), "", 2.2);
                Flash(Color.FromRgb(0xE0, 0x32, 0x2B), 0.5);
                break;
            case MatchEventKind.FinisherStart:
                var def = _context.Finishers[e.Slot][e.Value];
                Callout(FinisherFactory.KindName(def.Kind).ToString(), def.Name.ToString(), 3.5);
                SpeedLines(1.4);
                if (def.Kind == FinisherKind.Friendship) Confetti(3);
                else Flash(def.Kind == FinisherKind.Legendary ? Color.FromRgb(0xF6, 0xC2, 0x1B) : Color.FromRgb(0xE0, 0x32, 0x2B), 0.7);
                break;
            case MatchEventKind.Mercy:
                Callout(Loc.T("hud.mercy"), "", 2.4);
                Confetti(2);
                break;
            case MatchEventKind.MatchWin:
            case MatchEventKind.RoundWin when e.Value < 0:
                if (e.Value < 0) Callout(Loc.T("hud.draw"), "", 2);
                else if (e.Kind == MatchEventKind.MatchWin)
                {
                    Callout(Loc.T("hud.wins", WinnerName(state, e.Value)), "", 3);
                    Confetti(4);
                }
                break;
            case MatchEventKind.Combo when e.Target >= 0 && e.Slot >= 0 && e.Value >= 5:
                ShowSide(e.Slot, Loc.T("hud.combo", e.Value));
                break;
            case MatchEventKind.TeamCombo when e.Slot >= 0:
                ShowSide(e.Slot, Loc.T("hud.teamCombo"));
                Confetti(0.8);
                break;
            case MatchEventKind.RageActivated when e.Slot >= 0:
                ShowSide(e.Slot, Loc.T("hud.rage"));
                Flash(Color.FromRgb(0xE0, 0x32, 0x2B), 0.35);
                break;
            case MatchEventKind.AdrenalineActivated when e.Slot >= 0: ShowSide(e.Slot, Loc.T("hud.adrenaline")); break;
            case MatchEventKind.ArenaEvent:
                _eventToast.Text = _context.Stage.Events[e.Value].Name.ToString();
                _eventTimer = 2.5;
                _eventBox.Opacity = 1;
                _ = Anim.SlideIn(_eventBox, 0, 0, 320, "", -60);
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
        _ = Anim.Pop(p.Combo, 1.5, 200);
    }
}
