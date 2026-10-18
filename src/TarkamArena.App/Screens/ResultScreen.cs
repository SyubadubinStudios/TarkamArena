using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Progression;

namespace TarkamArena.App.Screens;

/// <summary>After a match: the winner on a poster, what the player earned, and where to go next.</summary>
public sealed class ResultScreen : Screen
{
    public ResultScreen(MatchPlan plan, MatchSummary summary, RewardSummary reward, CharacterDefinition fighter, string? continueLabel = null, Action? onContinue = null)
    {
        var verdict = new SignText(summary.Won ? (Loc.Current == Language.Indonesian ? "MENANG!" : "VICTORY!") : (Loc.Current == Language.Indonesian ? "KALAH" : "DEFEAT"), 110, summary.Won ? Ui.Spanduk : Ui.Kapur);
        var portrait = Ui.Picture(Images.Portrait(fighter, 720), height: 700);
        portrait.HorizontalAlignment = HorizontalAlignment.Right;
        portrait.VerticalAlignment = VerticalAlignment.Bottom;
        portrait.Margin = new Thickness(0, 0, 120, 0);

        var lines = new StackPanel { Spacing = 6 };
        void Line(string label, string value) => lines.Children.Add(Ui.Columns("220,*", Ui.Text(label.ToUpperInvariant(), "eyebrow"), Ui.Text(value, "h3")));
        Line("XP", $"+{reward.Xp}");
        Line(Loc.Current == Language.Indonesian ? "Koin" : "Coins", $"+{reward.Coins}");
        Line(Loc.T("hud.crowd"), $"+{reward.ReputationGained}");
        Line(Loc.T("hud.combo", "").Trim(), $"{summary.MaxCombo}");
        if (summary.Finisher is { } f) Line(Loc.T("select.finishers"), FinisherFactory.KindName(f).ToString());
        foreach (var level in reward.LevelsReached) lines.Children.Add(Ui.Text(Loc.T("result.levelUp", level), "h2").With(t => t.Foreground = Ui.Spanduk));
        foreach (var a in reward.NewAchievements) lines.Children.Add(Ui.Text($"★ {a.Name}", "h3").With(t => t.Foreground = Ui.Spanduk));
        foreach (var u in reward.NewUnlocks) lines.Children.Add(Ui.Text(Loc.T("result.unlocked", u.Name), "h3").With(t => t.Foreground = Ui.Cabai));

        var buttons = new StackPanel { Spacing = 10 };
        if (onContinue is not null)
        {
            var b = Ui.Banner((continueLabel ?? Loc.T("common.continue")).ToUpperInvariant(), onContinue);
            buttons.Children.Add(b);
            Nav(b);
        }
        else if (plan.Online is null)
        {
            var re = Ui.Banner(Loc.T("common.rematch").ToUpperInvariant(), () => { plan.Seed++; Window.Replace(new FightScreen(plan)); });
            buttons.Children.Add(re);
            Nav(re);
        }
        var menu = Ui.Banner(Loc.T("common.mainMenu").ToUpperInvariant(), () => Window.Home());
        buttons.Children.Add(menu);
        Nav(menu);

        var left = Ui.V(22, Ui.Text(Loc.T("result.title").ToUpperInvariant(), "eyebrow"), verdict,
            Ui.Text($"{fighter.Name} · {fighter.Title}", "h3", "muted"), Ui.Panel(lines, 22).With(p => p.Width = 560), buttons);
        left.Margin = new Thickness(90, 70, 0, 0);

        var root = new Grid();
        root.Children.Add(new Border { Background = summary.Won ? Ui.Spanduk : Ui.Cabai, Width = 420, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 200, 0), RenderTransform = new SkewTransform(-9, 0), Opacity = 0.9 });
        root.Children.Add(portrait);
        root.Children.Add(left);
        Content = root;
    }

    protected override bool Back()
    {
        Window.Home();
        return true;
    }
}
