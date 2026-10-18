using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using TarkamArena.App.Screens;
using TarkamArena.Core.AI;
using TarkamArena.Core.Content;
using TarkamArena.Core.Match;
using TarkamArena.Core.Modes;

namespace TarkamArena.App.Services;

/// <summary>
/// <c>--capture &lt;dir&gt;</c>: visits every screen (and a few fights between CPUs) and saves a
/// PNG of each, so README and documentation screenshots are reproducible from the real build.
/// </summary>
public static class ScreenshotTour
{
    public static async Task RunAsync(MainWindow window, string directory)
    {
        Directory.CreateDirectory(directory);
        // Windows throttles rendering for covered windows; stay on top for the length of the tour.
        window.Topmost = true;
        window.Activate();
        async Task Shot(string name, Screen? screen, double seconds = 1.5, bool replace = false)
        {
            if (screen is not null)
            {
                if (replace) window.Replace(screen);
                else window.Show(screen);
            }
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            await Dispatcher.UIThread.InvokeAsync(() => Save(window, Path.Combine(directory, name + ".png")), DispatcherPriority.Render);
        }

        await Shot("01-title", null, 2.5);
        await Shot("02-main-menu", new MainMenuScreen(), 2, replace: true);
        await Shot("03-mode-select", new ModeSelectScreen());
        await Shot("04-character-select", new CharacterSelectScreen(MatchPlan.For(GameMode.Versus)), 2);
        var stagePlan = MatchPlan.For(GameMode.Versus);
        stagePlan.Fighters[0] = Roster.Get("gembel-lampu-merah");
        stagePlan.Fighters[1] = Roster.Get("kuntilanak");
        await Shot("05-stage-select", new StageSelectScreen(stagePlan), 2);

        (string Name, string Stage, string[] Fighters, GameMode Mode, double Wait)[] fights =
        [
            ("06-fight-pasar-becek", "pasar-becek", ["gembel-lampu-merah", "preman-pasar"], GameMode.Versus, 9),
            ("07-fight-sound-horeg", "karnaval-sound-horeg", ["horeg-dancer", "reog-ponorogo"], GameMode.Versus, 9),
            ("08-fight-pengerupukan", "malam-pengerupukan", ["leak-bali", "ogoh-ogoh"], GameMode.Versus, 9),
            ("09-fight-2v2-demo", "jalan-demo", ["mahasiswi-demo", "anak-stm", "pejabat-korup", "buzzer-politik"], GameMode.TwoVsTwo, 10),
            ("10-fight-pantai-selatan", "pantai-selatan", ["kuntilanak", "nyi-roro-kidul"], GameMode.Versus, 9),
            ("11-fight-borobudur", "candi-borobudur", ["pesilat", "harimau-sumatra"], GameMode.Versus, 9),
        ];
        foreach (var f in fights)
        {
            var plan = MatchPlan.For(f.Mode);
            for (int i = 0; i < plan.SlotCount; i++)
            {
                plan.Controllers[i] = SlotController.Cpu;
                plan.Fighters[i] = Roster.Get(f.Fighters[i]);
            }
            plan.Difficulty = CpuDifficulty.Hard;
            plan.Stage = Stages.Get(f.Stage);
            plan.Seed = 4242;
            plan.Rules = plan.Rules with { ArenaEventsEnabled = false };
            // Loading time depends on the GPU; wait for the arena to be live, then let the fight run.
            var screen = new FightScreen(plan);
            window.Show(screen);
            await Task.WhenAny(screen.Ready, Task.Delay(TimeSpan.FromSeconds(150)));
            await Shot(f.Name, null, f.Wait - 3);
            window.Back();
        }

        // Signature moves: wait for a CPU to throw one, then catch the cut-in and the effect.
        {
            var plan = MatchPlan.For(GameMode.Versus);
            plan.Controllers = [SlotController.Cpu, SlotController.Cpu];
            plan.Fighters[0] = Roster.Get("kuyang");
            plan.Fighters[1] = Roster.Get("mahasiswi-demo");
            plan.Difficulty = CpuDifficulty.Legend;
            plan.Stage = Stages.Get("pasar-becek");
            plan.Seed = 77;
            plan.Rules = plan.Rules with { ArenaEventsEnabled = false };
            var fight = new FightScreen(plan) { FillJurus = true };
            var shown = new TaskCompletionSource();
            fight.SignatureShown += () => shown.TrySetResult();
            window.Show(fight);
            await Task.WhenAny(fight.Ready, Task.Delay(TimeSpan.FromSeconds(150)));
            await Task.WhenAny(shown.Task, Task.Delay(TimeSpan.FromSeconds(30)));
            await Shot("19-signature-cutin", null, 0.3);
            await Shot("20-signature-effect", null, 1.6);
            await Dispatcher.UIThread.InvokeAsync(fight.PauseForCapture);
            await Shot("21-pause-move-list", null, 0.8);
            window.Back();
        }

        await Shot("12-story", new StoryScreen(), 2);
        window.Back();
        await Shot("13-tournament", new TournamentScreen(new Tournament(Roster.Get("pesilat"), Game.PlayableRoster(), 7)), 2);
        window.Back();
        await Shot("14-creator", new CreatorScreen(), 3);
        window.Back();
        await Shot("15-collection", new CollectionScreen(), 1.5);
        window.Back();
        await Shot("16-settings", new SettingsScreen(), 1.5);
        window.Back();
        await Shot("17-online", new OnlineScreen(), 1.5);
        window.Back();
        await Shot("18-credits", new CreditsScreen(), 6);
        window.Back();
        window.Close();
    }

    /// <summary><c>--fight</c>: jump straight into a CPU-vs-player fight (development shortcut).</summary>
    public static void QuickFight(MainWindow window)
    {
        var plan = MatchPlan.For(GameMode.Versus);
        plan.Fighters[0] = Roster.Get(LaunchOptions.Fighters is { Length: > 0 } f0 ? f0[0] : "gembel-lampu-merah");
        plan.Fighters[1] = Roster.Get(LaunchOptions.Fighters is { Length: > 1 } f1 ? f1[1] : "preman-pasar");
        if (LaunchOptions.SignatureDemo)
        {
            plan.Controllers = [SlotController.Cpu, SlotController.Cpu];
            plan.Difficulty = CpuDifficulty.Legend;
        }
        plan.Stage = Stages.Get("pasar-becek");
        window.Replace(new MainMenuScreen());
        window.Show(new FightScreen(plan));
        if (LaunchOptions.SnapDirectory is { } dir)
        {
            // --snap <dir>: save in-app frames at fixed times (no desktop capture needed).
            Directory.CreateDirectory(dir);
            var shots = LaunchOptions.SignatureDemo
                ? Enumerable.Range(0, 24).Select(i => (8.0 + i * 0.5, $"sig-{i:00}")).ToArray()
                : new[] { (4.0, "a-vs-loaded"), (5.6, "b-round"), (8.0, "c-fight"), (12.0, "d-fight"), (18.0, "e-fight"), (26.0, "f-fight") };
            foreach (var (seconds, name) in shots)
                DispatcherTimer.RunOnce(() => Save(window, Path.Combine(dir, name + ".png")), TimeSpan.FromSeconds(seconds));
        }
    }

    private static void Save(MainWindow window, string path)
    {
        var size = new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height);
        using var bitmap = new RenderTargetBitmap(size);
        bitmap.Render(window);
        bitmap.Save(path);
    }
}
