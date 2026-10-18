using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;

namespace TarkamArena.App.Screens;

/// <summary>Key art, the painted title and "press Enter". The tournament poster before you walk in.</summary>
public sealed class TitleScreen : Screen
{
    private readonly TextBlock _prompt;
    private readonly DispatcherTimer _blink;

    public TitleScreen()
    {
        var art = Ui.Picture(Images.Load("ui/keyart.png", 1920), stretch: Stretch.UniformToFill);
        var shade = new Border
        {
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0xF2, 0x0A, 0x16, 0x38), 0),
                    new GradientStop(Color.FromArgb(0xB0, 0x0A, 0x16, 0x38), 0.42),
                    new GradientStop(Color.FromArgb(0x00, 0x0A, 0x16, 0x38), 0.75),
                },
            },
        };

        _prompt = Ui.Text(Loc.T("app.pressStart").ToUpperInvariant(), "h3").With(t => t.Foreground = Ui.Spanduk);
        var tagline = Ui.Wrap(Loc.T("app.tagline"), "h3").With(t => { t.Foreground = Ui.Kapur; t.MaxWidth = 560; });
        var slogan = Ui.Wrap(Loc.T("app.slogan"), "body", "muted").With(t => t.MaxWidth = 520);

        var title = Ui.V(0,
            new SignText("TARKAM", 132),
            new SignText("ARENA", 132, Ui.Kapur));

        var stack = Ui.V(22,
            Ui.Text("SYUBADUBIN STUDIOS  ·  2026", "eyebrow"),
            title,
            tagline,
            slogan,
            new Border { Height = 18 },
            _prompt);
        stack.Margin = new Thickness(96, 0, 0, 0);
        stack.VerticalAlignment = VerticalAlignment.Center;

        var footer = Footer().With(f =>
        {
            f.Margin = new Thickness(96, 0, 0, 36);
            f.VerticalAlignment = VerticalAlignment.Bottom;
        });

        var root = new Grid();
        root.Children.Add(art);
        root.Children.Add(shade);
        root.Children.Add(stack);
        root.Children.Add(footer);
        Content = root;

        _blink = new DispatcherTimer(TimeSpan.FromMilliseconds(600), DispatcherPriority.Background, (_, _) => _prompt.Opacity = _prompt.Opacity > 0.5 ? 0.25 : 1);
        PointerPressed += (_, _) => Enter();
    }

    public override void OnEnter()
    {
        _blink.Start();
        Game.Audio.StopMusic();
        _ = Game.Audio.StartMusicAsync(MusicStyle.Gamelan, 21);
    }

    public override void OnLeave() => _blink.Stop();

    public override bool HandleAction(MenuAction action)
    {
        if (action is MenuAction.Confirm) Enter();
        return true;
    }

    private void Enter()
    {
        Game.Audio.Play(Engine.Audio.Sfx.Bell, 0.8f, 0);
        Window.Replace(new MainMenuScreen());
    }
}
