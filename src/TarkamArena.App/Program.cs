using Avalonia;

namespace TarkamArena.App;

internal static class Program
{
    /// <summary>
    /// Command line:
    ///   --capture &lt;dir&gt;   walk through every screen and save screenshots (used for README and docs)
    ///   --fullscreen      start in full screen
    ///   --lang en|id      override the interface language
    /// </summary>
    [STAThread]
    public static int Main(string[] args)
    {
        LaunchOptions.Parse(args);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => CrashLog.Write(e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => CrashLog.Write(e.Exception);
        ThreeNet.ThreeNetRuntime.Initialize();
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}

internal static class LaunchOptions
{
    public static string? CaptureDirectory { get; private set; }
    public static bool Fullscreen { get; private set; }
    public static string? Language { get; private set; }
    public static bool QuickFight { get; private set; }
    public static string? SnapDirectory { get; private set; }

    /// <summary>--fighters a,b: the two fighters for --fight.</summary>
    public static string[]? Fighters { get; private set; }

    /// <summary>--jurus: CPU against CPU with full Jurus meters, to show off signature moves.</summary>
    public static bool SignatureDemo { get; private set; }

    public static void Parse(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--capture" when i + 1 < args.Length: CaptureDirectory = args[++i]; break;
                case "--fullscreen": Fullscreen = true; break;
                case "--fight": QuickFight = true; break;
                case "--snap" when i + 1 < args.Length: SnapDirectory = args[++i]; break;
                case "--lang" when i + 1 < args.Length: Language = args[++i]; break;
                case "--fighters" when i + 1 < args.Length: Fighters = args[++i].Split(','); break;
                case "--jurus": SignatureDemo = true; break;
            }
        }
    }
}

/// <summary>
/// Timing marks for diagnosing slow loads: set <c>TARKAM_TRACE=1</c> and read
/// <c>%TEMP%/tarkam-trace.txt</c>. Costs nothing when the variable is not set.
/// </summary>
internal static class LoadTrace
{
    private static readonly bool s_on = Environment.GetEnvironmentVariable("TARKAM_TRACE") == "1";
    private static readonly System.Diagnostics.Stopwatch s_clock = System.Diagnostics.Stopwatch.StartNew();

    public static void Mark(string what)
    {
        if (!s_on) return;
        System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tarkam-trace.txt"), $"{s_clock.ElapsedMilliseconds,8} ms  {what}{System.Environment.NewLine}");
    }
}

/// <summary>Writes unexpected exceptions next to the save file so players can attach them to bug reports.</summary>
internal static class CrashLog
{
    public static string PathName => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Persistence.GameDatabase.DefaultPath())!, "log.txt");

    public static void Write(Exception? e)
    {
        if (e is null) return;
        try
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathName)!);
            File.AppendAllText(PathName, $"[{DateTimeOffset.Now:O}] {e}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (IOException)
        {
        }
    }
}
