using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;

namespace TarkamArena.App.Screens;

/// <summary>Arena select: painted plates of every stage, with locals, events and music listed.</summary>
public sealed class StageSelectScreen : Screen
{
    private const int Columns = 4;
    private readonly MatchPlan _plan;
    private readonly Image _hero = new() { Stretch = Stretch.UniformToFill };
    private readonly SignText _name = new("", 48);
    private readonly TextBlock _region = Ui.Text("", "eyebrow");
    private readonly TextBlock _description = Ui.Wrap("", "body").With(t => t.MaxWidth = 640);
    private readonly TextBlock _facts = Ui.Wrap("", "body", "muted").With(t => t.MaxWidth = 640);
    private StageDefinition? _hover;

    public StageSelectScreen(MatchPlan plan)
    {
        _plan = plan;
        var grid = new UniformGrid { Columns = Columns };
        foreach (var stage in Stages.All)
        {
            var thumb = Ui.Picture(Images.Backdrop(stage, 360), 230, 99, Stretch.UniformToFill);
            var label = new TextBlock { Text = stage.Name.ToString().ToUpperInvariant(), FontFamily = Ui.Display, FontSize = 12, Foreground = Ui.Arang, TextTrimming = TextTrimming.CharacterEllipsis };
            var card = Ui.Rows("*,Auto", thumb, new Border { Background = Ui.Spanduk, Padding = new Thickness(6, 3), Child = label });
            var b = new Button { Content = card }.Classes("ticket");
            var s = stage;
            b.GotFocus += (_, _) => Show(s);
            b.Click += (_, _) => Go(s);
            grid.Children.Add(b);
            Nav(b);
        }

        var shade = new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.FromArgb(0x30, 0x0A, 0x16, 0x38), 0), new GradientStop(Color.FromArgb(0xF5, 0x0A, 0x16, 0x38), 0.62) },
            },
        };
        var info = Ui.V(8, Ui.Text(Loc.T("stage.title").ToUpperInvariant(), "eyebrow"), _region, _name, _description, _facts);
        info.Margin = new Thickness(56, 40, 56, 0);
        var list = new ScrollViewer { Content = grid, Margin = new Thickness(48, 0, 48, 28), VerticalScrollBarVisibility = ScrollBarVisibility.Auto };

        var root = new Grid();
        root.Children.Add(_hero);
        root.Children.Add(shade);
        root.Children.Add(Ui.Rows("Auto,*", info, list.With(l => l.VerticalAlignment = VerticalAlignment.Bottom)));
        Content = root;
    }

    private void Show(StageDefinition s)
    {
        _hover = s;
        _hero.Source = Images.Backdrop(s, 1600);
        _name.Text = s.Name.ToString().ToUpperInvariant();
        _region.Text = s.Region.ToUpperInvariant();
        _description.Text = s.Description.ToString();
        string npcs = string.Join(", ", s.Npcs.Select(n => n.ToString()));
        string events = string.Join(" · ", s.Events.Select(e => e.Name.ToString()));
        _facts.Text = $"{Loc.T("stage.npcs")}: {npcs}\n{Loc.T("stage.events")}: {events}\n{Loc.T("stage.music")}: {s.Music}";
    }

    private void Go(StageDefinition s)
    {
        _plan.Stage = s;
        Game.Audio.Play(Engine.Audio.Sfx.Bell, 0.8f, 0);
        Window.Replace(new FightScreen(_plan));
    }

    public override bool HandleAction(MenuAction action)
    {
        switch (action)
        {
            case MenuAction.Up when NavIndex >= Columns: NavIndex -= Columns; FocusCurrent(); return true;
            case MenuAction.Down when NavIndex + Columns < NavItems.Count: NavIndex += Columns; FocusCurrent(); return true;
            case MenuAction.Up or MenuAction.Down: return true;
            case MenuAction.Confirm when _hover is not null: Go(_hover); return true;
        }
        return base.HandleAction(action);
    }
}
