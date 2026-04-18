using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using HeyManNiceShot.Capture;
using HeyManNiceShot.Hotkeys;
using HeyManNiceShot.Settings;
using HeyManNiceShot.Tray;

namespace HeyManNiceShot;

public partial class App : Application
{
    public static new App Current => (App)Application.Current!;

    public SettingsStore SettingsStore { get; private set; } = null!;
    public AppSettings Settings { get; private set; } = null!;
    public CaptureService Capture { get; private set; } = null!;
    public HotkeyService Hotkeys { get; private set; } = null!;
    public TrayHost Tray { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        WireCrashHandlers();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.MainWindow = null;

            SettingsStore = new SettingsStore();
            Settings = SettingsStore.Load();

            Capture = new CaptureService();
            Hotkeys = new HotkeyService();
            Hotkeys.Pressed += TriggerCapture;
            Hotkeys.Register(Settings.CaptureHotkey);

            Tray = new TrayHost();
            Tray.CaptureRequested += TriggerCapture;
            Tray.SettingsRequested += OpenSettings;
            Tray.QuitRequested += () => desktop.Shutdown();
            Tray.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void ApplySettings(AppSettings newSettings)
    {
        Settings = newSettings;
        SettingsStore.Save(newSettings);
        Hotkeys.Register(newSettings.CaptureHotkey);
    }

    private void TriggerCapture() => CaptureOrchestrator.Run(this);

    private void OpenSettings()
    {
        var window = new SettingsWindow();
        window.Show();
    }

    private void WireCrashHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            CrashLog.Write("AppDomain", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
            CrashLog.Write("Task", e.Exception);
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            CrashLog.Write("Dispatcher", e.Exception);
            e.Handled = true;
        };
    }
}
