using System.Windows;

namespace rans0m;

public partial class App : Application
{
    private Mutex? _instanceMutex;
    private bool _ownsMutex;

    public App()
    {
        // Observe unexpected failures without concealing or suppressing them.
        DispatcherUnhandledException += (_, e) => LabDiagnostics.Write("WPF exception: " + e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            LabDiagnostics.Write("Fatal exception: " + e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, e) =>
            LabDiagnostics.Write("Task exception: " + e.Exception);
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LabDiagnostics.Write("Application started");
        _instanceMutex = new Mutex(true, @"Local\Rans0mAwarenessLab", out bool createdNew);
        _ownsMutex = createdNew;
        if (!createdNew) { Shutdown(); return; }

        // Recover only our previous work before waiting for the first real interaction.
        WallpaperUpdater.RestoreWallpaper();
        GoldCoinManager.RecoverOrphanedSessions();
        Config.Load();
        try
        {
            await ActivityGate.WaitForNextActivityAsync(TimeSpan.FromSeconds(3));
            LabDiagnostics.Write("User activity detected, opening simulation");
            new Overlay().Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "RANS0M Lab — Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LabDiagnostics.Write($"Application exiting, code={e.ApplicationExitCode}");
        GoldCoinManager.DeleteAllCoins();
        WallpaperUpdater.RestoreWallpaper();
        if (_ownsMutex) _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
