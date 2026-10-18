using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;

namespace TarkamArena.App.Screens;

/// <summary>
/// The tournament poster before you walk in: drifting key art, rising embers, the painted title
/// stamped onto the screen letter-block by letter-block, and a breathing "press Enter".
/// </summary>
public sealed class TitleScreen : Screen
{
    private readonly TextBlock _prompt;
    private readonly Image _art;
    private readonly SignText _title1 = new("TARKAM", 132);
    private readonly SignText _title2 = new("ARENA", 132, Ui.Kapur);
    private readonly TextBlock _tagline;
    private readonly TextBlock _slogan;
    private readonly TextBlock _eyebrow = Ui.Text("SYUBADUBIN STUDIOS  ·  2026", "eyebrow");
    private bool _leaving;

    protected override bool StaggerNavItems => false;

    public TitleScreen()
    {
        _art = Ui.Picture(Images.Load("ui/keyart.png", 1920), stretch: Stretch.UniformToFill);
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
        _tagline = Ui.Wrap(Loc.T("app.tagline"), "h3").With(t => { t.Foreground = Ui.Kapur; t.MaxWidth = 560; });
        _slogan = Ui.Wrap(Loc.T("app.slogan"), "body", "muted").With(t => t.MaxWidth = 520);

        var stack = Ui.V(22, _eyebrow, Ui.V(0, _title1, _title2), _tagline, _slogan, new Border { Height = 18 }, _prompt);
        stack.Margin = new Thickness(96, 0, 0, 0);
        stack.VerticalAlignment = VerticalAlignment.Center;

        var mendung = Ui.Picture(Images.Load("ui/mega-mendung.png", 1600), stretch: Stretch.UniformToFill)
            .With(i => { i.Height = 70; i.VerticalAlignment = VerticalAlignment.Bottom; i.Opacity = 0.55; });
        var footer = Footer().With(f =>
        {
            f.Margin = new Thickness(96, 0, 0, 84);
            f.VerticalAlignment = VerticalAlignment.Bottom;
        });

        var root = new Grid { ClipToBounds = true };
        root.Children.Add(_art);
        root.Children.Add(shade);
        root.Children.Add(new ParticleField(ParticleStyle.Embers, 110));
        root.Children.Add(stack);
        root.Children.Add(mendung);
        root.Children.Add(footer);
        Content = root;
        PointerPressed += (_, _) => Enter();
    }

    protected override void AnimateEntrance()
    {
        Anim.Drift(_art, 30, 1.08, 30);
        _ = Anim.FadeIn(_eyebrow, 150, 500);
        _ = Anim.Stamp(_title1, 350, 460);
        _ = Anim.Stamp(_title2, 620, 460, 2.4, 6);
        _ = Anim.SlideIn(_tagline, -40, 950, 420);
        _ = Anim.FadeIn(_slogan, 1150, 600);
        _ = Anim.FadeIn(_prompt, 1450, 400).ContinueWith(_ => Avalonia.Threading.Dispatcher.UIThread.Post(() => Anim.Pulse(_prompt)));
        Avalonia.Threading.DispatcherTimer.RunOnce(() => Game.Audio.Play(Engine.Audio.Sfx.HitHeavy, 0.6f, 0), TimeSpan.FromMilliseconds(520));
        Avalonia.Threading.DispatcherTimer.RunOnce(() => Game.Audio.Play(Engine.Audio.Sfx.HitHeavy, 0.6f, 0.05f), TimeSpan.FromMilliseconds(790));
    }

    public override void OnEnter()
    {
        base.OnEnter();
        Game.Audio.StopMusic();
        _ = Game.Audio.StartMusicAsync(MusicStyle.Gamelan, 21);
    }

    public override bool HandleAction(MenuAction action)
    {
        if (action is MenuAction.Confirm) Enter();
        return true;
    }

    private void Enter()
    {
        if (_leaving) return;
        _leaving = true;
        Game.Audio.Play(Engine.Audio.Sfx.Bell, 0.8f, 0);
        _ = Anim.Pop(_prompt, 1.25, 180);
        Window.Replace(new MainMenuScreen());
    }
}
