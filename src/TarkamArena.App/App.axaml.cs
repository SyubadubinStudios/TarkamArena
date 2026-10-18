using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TarkamArena.App.Services;

namespace TarkamArena.App;

public partial class App : Application
{
    public static MainWindow? CurrentWindow =>
        (Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow as MainWindow;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (_, e) =>
            {
                CrashLog.Write(e.Exception);
                e.Handled = true;
                CurrentWindow?.Toast("Error", e.Exception.Message);
            };
            Controls.Anim.Register();
            Game.Initialize();
            desktop.MainWindow = new MainWindow();
            desktop.ShutdownRequested += (_, _) => Game.Shutdown();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
