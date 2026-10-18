using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.AI;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;
using TarkamArena.Core.Modes;
using TarkamArena.Core.Progression;

namespace TarkamArena.App.Screens;

/// <summary>The Tarkam Cup bracket: quarter-finals, semis and the final, painted like a kampung noticeboard.</summary>
public sealed class TournamentScreen : Screen
{
    private readonly Tournament _cup;

    public TournamentScreen(Tournament cup)
    {
        _cup = cup;
        Build();
    }

    private void Build()
    {
        NavItems.Clear();
        string[] titles = [Loc.T("tournament.quarter"), Loc.T("tournament.semi"), Loc.T("tournament.final")];
        var columns = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*,*,*,Auto"), ColumnSpacing = 30 };
        for (int r = 0; r < Tournament.Rounds; r++)
        {
            var col = new StackPanel { Spacing = 18, VerticalAlignment = VerticalAlignment.Center };
            col.Children.Add(Ui.Text(titles[r].ToUpperInvariant(), "eyebrow"));
            var round = r < _cup.Bracket.Count ? _cup.Bracket[r] : [];
            int expected = Tournament.Entrants >> (r + 1);
            for (int m = 0; m < expected; m++)
            {
                var match = m < round.Count ? round[m] : null;
                col.Children.Add(MatchCard(match));
            }
            Grid.SetColumn(col, r);
            columns.Children.Add(col);
        }

        var champion = Ui.V(10, Ui.Text(Loc.T("tournament.champion").ToUpperInvariant(), "eyebrow"),
            Ui.Picture(_cup.Champion is null ? null : Images.Portrait(_cup.Champion, 300), 240, 300),
            new SignText(_cup.Champion?.Name.ToString().ToUpperInvariant() ?? "?", 30));
        champion.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(champion, 3);
        columns.Children.Add(champion);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
        if (_cup.NextPlayerMatch is not null)
        {
            var b = Ui.Banner(Loc.T("tournament.next").ToUpperInvariant(), PlayNext);
            actions.Children.Add(b);
            Nav(b);
        }
        else
        {
            var again = Ui.Banner(Loc.T("tournament.new").ToUpperInvariant(), () => Window.Replace(CharacterSelectScreen.ForTournament()));
            actions.Children.Add(again);
            Nav(again);
        }
        var home = Ui.Banner(Loc.T("common.mainMenu").ToUpperInvariant(), () => Window.Home(), small: true);
        actions.Children.Add(home);
        Nav(home);

        var root = Ui.Rows("Auto,*,Auto", new SignText(Loc.T("tournament.title").ToUpperInvariant(), 60), columns, actions);
        root.Margin = new Thickness(70, 50, 70, 50);
        Content = root;
    }

    private static Control MatchCard(BracketMatch? m)
    {
        Control Row(CharacterDefinition? c)
        {
            bool won = m?.Winner is not null && m.Winner == c;
            bool lost = m?.Winner is not null && c is not null && m.Winner != c;
            var row = Ui.H(10,
                Ui.Picture(c is null ? null : Images.Portrait(c, 96), 44, 44, Stretch.UniformToFill),
                Ui.Text(c?.Name.ToString() ?? "—", "h3").With(t => t.Opacity = lost ? 0.4 : 1));
            return new Border { Child = row, Padding = new Thickness(8, 4), Background = won ? Ui.Spanduk : Brushes.Transparent }
                .With(b => { if (won && row.Children[1] is TextBlock t) t.Foreground = Ui.Arang; });
        }
        var card = Ui.V(2, Row(m?.Left), Row(m?.Right));
        return new Border
        {
            Child = card,
            Background = Ui.PanelFill,
            BorderBrush = m?.IsPlayerMatch == true ? Ui.Cabai : Ui.Line,
            BorderThickness = new Thickness(m?.IsPlayerMatch == true ? 3 : 1),
            Width = 300,
        };
    }

    private void PlayNext()
    {
        var match = _cup.NextPlayerMatch!;
        var opponent = match.Left == _cup.Player ? match.Right! : match.Left!;
        var plan = MatchPlan.For(GameMode.Tournament);
        plan.Fighters[0] = _cup.Player;
        plan.Fighters[1] = opponent;
        plan.Difficulty = (CpuDifficulty)Math.Min(3, _cup.CurrentRound + 1);
        plan.Stage = Stages.All[Random.Shared.Next(Stages.All.Count)];
        plan.OpponentsFixed = true;
        plan.StageFixed = true;
        plan.OnFinished = (summary, _) =>
        {
            _cup.ReportPlayerResult(summary.Won);
            if (_cup.Champion == _cup.Player)
            {
                Game.Profile.TournamentWins++;
                Window.ToastRewards(ProgressionService.Refresh(Game.Profile));
                Game.Save();
            }
            Build();
            Window.Back();
            return true;
        };
        Window.Show(new FightScreen(plan));
    }

    protected override bool Back()
    {
        Window.Home();
        return true;
    }
}

/// <summary>Survival: one opponent after another, health carried over, until you fall.</summary>
public sealed class SurvivalScreen : Screen
{
    private readonly SurvivalRun _run;

    public SurvivalScreen(SurvivalRun run)
    {
        _run = run;
        Build();
    }

    private void Build()
    {
        NavItems.Clear();
        var opponent = _run.CurrentOpponent;
        var title = _run.IsOver ? Loc.T("tournament.eliminated") : Loc.T("survival.wave", _run.Wave);
        var actions = new StackPanel { Spacing = 10 };
        if (!_run.IsOver)
        {
            var go = Ui.Banner(Loc.T("common.start").ToUpperInvariant(), Play);
            actions.Children.Add(go);
            Nav(go);
        }
        var home = Ui.Banner(Loc.T("common.mainMenu").ToUpperInvariant(), () => Window.Home(), small: true);
        actions.Children.Add(home);
        Nav(home);

        var info = Ui.V(18,
            Ui.Text(Loc.T("survival.title").ToUpperInvariant(), "eyebrow"),
            new SignText(title.ToUpperInvariant(), 80),
            Ui.Wrap(Loc.T("survival.desc"), "body", "muted").With(t => t.MaxWidth = 560),
            Ui.Text(Loc.T("survival.best", Math.Max(Game.Profile.SurvivalBest, _run.Wave - 1)), "h3"),
            Ui.Text($"{_run.Player.Name}: {_run.HealthPercent}%", "h2").With(t => t.Foreground = _run.HealthPercent < 30 ? Ui.Cabai : Ui.Spanduk),
            Ui.Text($"{Loc.T("mode.opponent")}: {opponent.Name} · {CpuBrain.Name(_run.Difficulty)}", "h3", "muted"),
            actions);
        info.Margin = new Thickness(90, 70, 0, 0);
        var root = new Grid();
        root.Children.Add(Ui.Picture(Images.Portrait(opponent, 720), height: 700).With(i => { i.HorizontalAlignment = HorizontalAlignment.Right; i.VerticalAlignment = VerticalAlignment.Bottom; i.Margin = new Thickness(0, 0, 120, 0); }));
        root.Children.Add(info);
        Content = root;
    }

    private void Play()
    {
        var plan = MatchPlan.For(GameMode.Survival);
        plan.Fighters[0] = _run.Player;
        plan.Fighters[1] = _run.CurrentOpponent;
        plan.StartingHealth[0] = _run.HealthPercent;
        plan.Difficulty = _run.Difficulty;
        plan.Stage = Stages.All[(_run.Wave * 7) % Stages.All.Count];
        plan.OpponentsFixed = true;
        plan.StageFixed = true;
        plan.OnFinished = (summary, _) =>
        {
            _run.ReportResult(summary.Won, summary.Won ? Math.Max(1, (int)(LastHealth * 100)) : 0);
            int reached = _run.IsOver ? _run.Wave : _run.Wave - 1;
            if (reached > Game.Profile.SurvivalBest)
            {
                Game.Profile.SurvivalBest = reached;
                Window.ToastRewards(ProgressionService.Refresh(Game.Profile));
                Game.Save();
            }
            Build();
            Window.Back();
            return true;
        };
        Window.Show(new FightScreen(plan) { HealthReporter = h => LastHealth = h });
    }

    private double LastHealth { get; set; } = 1;

    protected override bool Back()
    {
        Window.Home();
        return true;
    }
}
