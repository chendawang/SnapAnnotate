using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ScreenshotAssistant.Capture;

namespace ScreenshotAssistant.App;

public sealed partial class App : Application, IDisposable
{
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private MainWindow? _mainWindow;
    private FloatingToolbarWindow? _floatingToolbarWindow;
    private ShareXGlobalHotkeyService? _hotkeyService;
    private ShareXHistoryService? _historyService;
    private ShareXTrayIconService? _trayIconService;
    private ShareXSettingsService? _settingsService;
    private ShareXStartupService? _startupService;
    private HttpClient? _updateHttpClient;
    private bool _disposed;

    internal static ShareXSingleInstanceService? SingleInstanceService { get; set; }

    internal static bool LaunchInBackground { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException(
                    "The M1 capture backend currently supports Windows only.");
            }

            _desktop = desktop;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _settingsService = ShareXSettingsService.LoadDefault();
            _historyService = ShareXHistoryService.CreateDefault(
                _settingsService.Current.ImageHistory);
            ShareXRegionCaptureWorkflow captureWorkflow = new(_historyService, _settingsService);
            _hotkeyService = new ShareXGlobalHotkeyService(_settingsService.Current.CaptureHotkey);
            string executablePath = Environment.ProcessPath ??
                throw new InvalidOperationException("无法确定当前程序路径。");
            _startupService = new ShareXStartupService(executablePath);
            _updateHttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            Version currentVersion = typeof(App).Assembly.GetName().Version ?? new Version(0, 1, 0);
            GitHubUpdateService updateService = new(
                _updateHttpClient,
                "chendawang",
                "SnapAnnotate",
                currentVersion);
            _mainWindow = new MainWindow(
                captureWorkflow,
                _hotkeyService,
                _historyService,
                _settingsService,
                _startupService,
                updateService);
            _mainWindow.ApplicationExitRequested += MainWindow_ApplicationExitRequested;
            _mainWindow.SettingsChanged += MainWindow_SettingsChanged;
            _mainWindow.FloatingToolbarRequested += MainWindow_FloatingToolbarRequested;
            _mainWindow.CaptureStarting += MainWindow_CaptureStarting;
            _mainWindow.CaptureFinished += MainWindow_CaptureFinished;
            desktop.MainWindow = _mainWindow;

            _floatingToolbarWindow = new FloatingToolbarWindow();
            _floatingToolbarWindow.CaptureRequested += FloatingToolbarWindow_CaptureRequested;
            _floatingToolbarWindow.MainWindowRequested += FloatingToolbarWindow_MainWindowRequested;
            _floatingToolbarWindow.CloseRequested += FloatingToolbarWindow_CloseRequested;

            _trayIconService = new ShareXTrayIconService(
                _hotkeyService.DisplayText,
                _settingsService.Current.CloseToTray,
                _startupService.IsEnabled,
                _settingsService.Current.ShowFloatingToolbar);
            SubscribeTrayEvents(_trayIconService);
            if (SingleInstanceService is not null)
            {
                SingleInstanceService.ActivationRequested += SingleInstanceService_ActivationRequested;
            }

            desktop.Exit += Desktop_Exit;
            ApplyFloatingToolbarVisibility();
            if (LaunchInBackground)
            {
                _mainWindow.ShowInTaskbar = false;
                _mainWindow.WindowState = WindowState.Minimized;
                _mainWindow.Opened += MainWindow_BackgroundOpened;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SubscribeTrayEvents(ShareXTrayIconService trayIconService)
    {
        trayIconService.ShowRequested += TrayIconService_ShowRequested;
        trayIconService.CaptureToFileRequested += TrayIconService_CaptureToFileRequested;
        trayIconService.CaptureToClipboardRequested += TrayIconService_CaptureToClipboardRequested;
        trayIconService.FullscreenCaptureRequested += TrayIconService_FullscreenCaptureRequested;
        trayIconService.OpenCaptureFolderRequested += TrayIconService_OpenCaptureFolderRequested;
        trayIconService.OpenHistoryRequested += TrayIconService_OpenHistoryRequested;
        trayIconService.CloseToTrayChanged += TrayIconService_CloseToTrayChanged;
        trayIconService.StartWithWindowsChanged += TrayIconService_StartWithWindowsChanged;
        trayIconService.ShowFloatingToolbarChanged += TrayIconService_ShowFloatingToolbarChanged;
        trayIconService.SettingsRequested += TrayIconService_SettingsRequested;
        trayIconService.ExitRequested += TrayIconService_ExitRequested;
    }

    private void UnsubscribeTrayEvents(ShareXTrayIconService trayIconService)
    {
        trayIconService.ShowRequested -= TrayIconService_ShowRequested;
        trayIconService.CaptureToFileRequested -= TrayIconService_CaptureToFileRequested;
        trayIconService.CaptureToClipboardRequested -= TrayIconService_CaptureToClipboardRequested;
        trayIconService.FullscreenCaptureRequested -= TrayIconService_FullscreenCaptureRequested;
        trayIconService.OpenCaptureFolderRequested -= TrayIconService_OpenCaptureFolderRequested;
        trayIconService.OpenHistoryRequested -= TrayIconService_OpenHistoryRequested;
        trayIconService.CloseToTrayChanged -= TrayIconService_CloseToTrayChanged;
        trayIconService.StartWithWindowsChanged -= TrayIconService_StartWithWindowsChanged;
        trayIconService.ShowFloatingToolbarChanged -= TrayIconService_ShowFloatingToolbarChanged;
        trayIconService.SettingsRequested -= TrayIconService_SettingsRequested;
        trayIconService.ExitRequested -= TrayIconService_ExitRequested;
    }

    private void SingleInstanceService_ActivationRequested(string[] arguments) =>
        Dispatcher.UIThread.Post(ShowMainWindow);

    private void MainWindow_BackgroundOpened(object? sender, EventArgs eventArgs)
    {
        if (_mainWindow is null)
        {
            return;
        }

        _mainWindow.Opened -= MainWindow_BackgroundOpened;
        _mainWindow.Hide();
        _mainWindow.ShowInTaskbar = true;
        _mainWindow.WindowState = WindowState.Normal;
    }

    private void TrayIconService_ShowRequested() => Dispatcher.UIThread.Post(ShowMainWindow);

    private void TrayIconService_CaptureToFileRequested() =>
        RunOnUiThread(() => _mainWindow?.RunCaptureFromExternalAsync(ShareXCaptureAction.RegionToFile));

    private void TrayIconService_CaptureToClipboardRequested() =>
        RunOnUiThread(() => _mainWindow?.RunCaptureFromExternalAsync(ShareXCaptureAction.RegionToClipboard));

    private void TrayIconService_FullscreenCaptureRequested() =>
        RunOnUiThread(() => _mainWindow?.RunCaptureFromExternalAsync(ShareXCaptureAction.FullscreenToFile));

    private void TrayIconService_OpenCaptureFolderRequested() => Dispatcher.UIThread.Post(() =>
    {
        if (_settingsService is not null)
        {
            ShareXShellService.OpenFolder(_settingsService.Current.SaveDirectory);
        }
    });

    private void TrayIconService_OpenHistoryRequested() => Dispatcher.UIThread.Post(() =>
    {
        ShowMainWindow();
        _mainWindow?.ShowHistoryPage();
    });

    private void TrayIconService_CloseToTrayChanged(bool enabled) => Dispatcher.UIThread.Post(() =>
    {
        if (_settingsService is null || _trayIconService is null)
        {
            return;
        }

        bool previous = _settingsService.Current.CloseToTray;
        _settingsService.Current.CloseToTray = enabled;
        if (!_settingsService.Save())
        {
            _settingsService.Current.CloseToTray = previous;
            _trayIconService.UpdateSystemPreferences(
                previous,
                _startupService?.IsEnabled == true,
                _settingsService.Current.ShowFloatingToolbar);
            _trayIconService.ShowError("关闭到托盘设置保存失败。");
        }
    });

    private void TrayIconService_StartWithWindowsChanged(bool enabled) => Dispatcher.UIThread.Post(() =>
    {
        if (_settingsService is null || _startupService is null || _trayIconService is null)
        {
            return;
        }

        bool previous = _startupService.IsEnabled;
        if (!_startupService.TrySetEnabled(enabled))
        {
            _trayIconService.UpdateSystemPreferences(
                _settingsService.Current.CloseToTray,
                previous,
                _settingsService.Current.ShowFloatingToolbar);
            _trayIconService.ShowError("开机启动设置失败，请检查启动目录权限。");
            return;
        }

        _settingsService.Current.StartWithWindows = enabled;
        if (!_settingsService.Save())
        {
            _startupService.TrySetEnabled(previous);
            _settingsService.Current.StartWithWindows = previous;
            _trayIconService.UpdateSystemPreferences(
                _settingsService.Current.CloseToTray,
                previous,
                _settingsService.Current.ShowFloatingToolbar);
            _trayIconService.ShowError("开机启动设置保存失败。");
        }
    });

    private void TrayIconService_ShowFloatingToolbarChanged(bool enabled) =>
        Dispatcher.UIThread.Post(() => SetFloatingToolbarEnabled(enabled));

    private void TrayIconService_SettingsRequested() => Dispatcher.UIThread.Post(() =>
    {
        ShowMainWindow();
        _mainWindow?.ShowSettingsPage();
    });

    private void TrayIconService_ExitRequested() => Dispatcher.UIThread.Post(ExitApplication);

    private void MainWindow_ApplicationExitRequested(object? sender, EventArgs eventArgs) => ExitApplication();

    private void MainWindow_SettingsChanged(object? sender, EventArgs eventArgs)
    {
        if (_trayIconService is null ||
            _settingsService is null ||
            _startupService is null ||
            _hotkeyService is null)
        {
            return;
        }

        _trayIconService.UpdateSystemPreferences(
            _settingsService.Current.CloseToTray,
            _startupService.IsEnabled,
            _settingsService.Current.ShowFloatingToolbar);
        _trayIconService.UpdateHotkeyDisplay(_hotkeyService.DisplayText);
        ApplyFloatingToolbarVisibility();
    }

    private void MainWindow_FloatingToolbarRequested(object? sender, EventArgs eventArgs)
    {
        SetFloatingToolbarEnabled(true);
        _mainWindow?.Hide();
    }

    private void MainWindow_CaptureStarting(object? sender, EventArgs eventArgs) =>
        _floatingToolbarWindow?.Hide();

    private void MainWindow_CaptureFinished(object? sender, EventArgs eventArgs) =>
        ApplyFloatingToolbarVisibility();

    private void FloatingToolbarWindow_CaptureRequested(object? sender, EventArgs eventArgs) =>
        RunOnUiThread(() => _mainWindow?.RunCaptureFromExternalAsync(ShareXCaptureAction.RegionToFile));

    private void FloatingToolbarWindow_MainWindowRequested(object? sender, EventArgs eventArgs) =>
        ShowMainWindow();

    private void FloatingToolbarWindow_CloseRequested(object? sender, EventArgs eventArgs) =>
        SetFloatingToolbarEnabled(false);

    private void SetFloatingToolbarEnabled(bool enabled)
    {
        if (_settingsService is null || _trayIconService is null)
        {
            return;
        }

        bool previous = _settingsService.Current.ShowFloatingToolbar;
        _settingsService.Current.ShowFloatingToolbar = enabled;
        if (!_settingsService.Save())
        {
            _settingsService.Current.ShowFloatingToolbar = previous;
            _trayIconService.ShowError("悬浮工具设置保存失败。");
        }

        _trayIconService.UpdateSystemPreferences(
            _settingsService.Current.CloseToTray,
            _startupService?.IsEnabled == true,
            _settingsService.Current.ShowFloatingToolbar);
        _mainWindow?.ReloadSettings();
        ApplyFloatingToolbarVisibility();
    }

    private void ApplyFloatingToolbarVisibility()
    {
        if (_floatingToolbarWindow is null || _settingsService is null)
        {
            return;
        }

        if (_settingsService.Current.ShowFloatingToolbar)
        {
            if (!_floatingToolbarWindow.IsVisible)
            {
                _floatingToolbarWindow.Show();
            }
        }
        else
        {
            _floatingToolbarWindow.Hide();
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void ExitApplication()
    {
        _mainWindow?.AllowApplicationExit();
        _desktop?.Shutdown();
    }

    private static void RunOnUiThread(Func<Task?> action) =>
        Dispatcher.UIThread.Post(async () =>
        {
            Task? task = action();
            if (task is not null)
            {
                await task;
            }
        });

    private void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs eventArgs)
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_desktop is not null)
        {
            _desktop.Exit -= Desktop_Exit;
        }

        if (SingleInstanceService is not null)
        {
            SingleInstanceService.ActivationRequested -= SingleInstanceService_ActivationRequested;
        }

        if (_mainWindow is not null)
        {
            _mainWindow.ApplicationExitRequested -= MainWindow_ApplicationExitRequested;
            _mainWindow.SettingsChanged -= MainWindow_SettingsChanged;
            _mainWindow.FloatingToolbarRequested -= MainWindow_FloatingToolbarRequested;
            _mainWindow.CaptureStarting -= MainWindow_CaptureStarting;
            _mainWindow.CaptureFinished -= MainWindow_CaptureFinished;
        }

        if (_floatingToolbarWindow is not null)
        {
            _floatingToolbarWindow.CaptureRequested -= FloatingToolbarWindow_CaptureRequested;
            _floatingToolbarWindow.MainWindowRequested -= FloatingToolbarWindow_MainWindowRequested;
            _floatingToolbarWindow.CloseRequested -= FloatingToolbarWindow_CloseRequested;
            _floatingToolbarWindow.Close();
            _floatingToolbarWindow = null;
        }

        if (_trayIconService is not null)
        {
            UnsubscribeTrayEvents(_trayIconService);
            _trayIconService.Dispose();
        }

        _hotkeyService?.Dispose();
        _historyService?.Dispose();
        _updateHttpClient?.Dispose();
    }
}
