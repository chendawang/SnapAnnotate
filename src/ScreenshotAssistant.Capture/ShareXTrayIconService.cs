using System.Windows.Forms;
using System.Drawing;
using ShareX.AvaloniaUI.Theming;
using ShareX.HelpersLib;

namespace ScreenshotAssistant.Capture;

public sealed class ShareXTrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly IDisposable _iconBinding;
    private readonly ToolStripMenuItem _closeToTrayItem;
    private readonly ToolStripMenuItem _startWithWindowsItem;
    private readonly ToolStripMenuItem _showFloatingToolbarItem;
    private readonly ToolStripMenuItem _captureToClipboardItem;
    private bool _disposed;

    public ShareXTrayIconService(
        string hotkeyDisplayText,
        bool closeToTray,
        bool startWithWindows,
        bool showFloatingToolbar)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hotkeyDisplayText);

        _menu = new ContextMenuStrip();
        ToolStripMenuItem showItem = CreateMenuItem("显示主界面", (_, _) => ShowRequested?.Invoke());
        showItem.Font = new System.Drawing.Font(showItem.Font, System.Drawing.FontStyle.Bold);
        _menu.Items.Add(showItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(CreateMenuItem("截图到文件", (_, _) => CaptureToFileRequested?.Invoke()));
        _captureToClipboardItem = CreateMenuItem(
            $"截图到剪切板    {hotkeyDisplayText}",
            (_, _) => CaptureToClipboardRequested?.Invoke());
        _menu.Items.Add(_captureToClipboardItem);
        _menu.Items.Add(CreateMenuItem("整屏截图", (_, _) => FullscreenCaptureRequested?.Invoke()));
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(CreateMenuItem("打开截图目录", (_, _) => OpenCaptureFolderRequested?.Invoke()));
        _menu.Items.Add(CreateMenuItem("打开完整历史", (_, _) => OpenHistoryRequested?.Invoke()));
        _menu.Items.Add(new ToolStripSeparator());
        _closeToTrayItem = CreateToggleMenuItem(
            "关闭主窗口时驻留托盘",
            closeToTray,
            CloseToTrayItem_Click);
        _startWithWindowsItem = CreateToggleMenuItem(
            "登录 Windows 后自动启动",
            startWithWindows,
            StartWithWindowsItem_Click);
        _showFloatingToolbarItem = CreateToggleMenuItem(
            "显示悬浮工具",
            showFloatingToolbar,
            ShowFloatingToolbarItem_Click);
        _menu.Items.Add(_closeToTrayItem);
        _menu.Items.Add(_startWithWindowsItem);
        _menu.Items.Add(_showFloatingToolbarItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(CreateMenuItem("设置", (_, _) => SettingsRequested?.Invoke()));
        _menu.Items.Add(CreateMenuItem("退出", (_, _) => ExitRequested?.Invoke()));

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Text = "截图助手",
            Visible = false
        };
        _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        Icon? applicationIcon = Environment.ProcessPath is { Length: > 0 } processPath
            ? Icon.ExtractAssociatedIcon(processPath)
            : null;
        if (applicationIcon is not null)
        {
            _notifyIcon.Icon = applicationIcon;
            _iconBinding = applicationIcon;
        }
        else
        {
            _iconBinding = LucideTrayIcon.Bind(_notifyIcon, LucideIcons.camera);
        }
        _notifyIcon.Visible = true;
    }

    public event Action? ShowRequested;

    public event Action? CaptureToFileRequested;

    public event Action? CaptureToClipboardRequested;

    public event Action? FullscreenCaptureRequested;

    public event Action? OpenCaptureFolderRequested;

    public event Action? OpenHistoryRequested;

    public event Action<bool>? CloseToTrayChanged;

    public event Action<bool>? StartWithWindowsChanged;

    public event Action<bool>? ShowFloatingToolbarChanged;

    public event Action? SettingsRequested;

    public event Action? ExitRequested;

    public void UpdateSystemPreferences(
        bool closeToTray,
        bool startWithWindows,
        bool showFloatingToolbar)
    {
        _closeToTrayItem.Checked = closeToTray;
        _startWithWindowsItem.Checked = startWithWindows;
        _showFloatingToolbarItem.Checked = showFloatingToolbar;
    }

    public void UpdateHotkeyDisplay(string hotkeyDisplayText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hotkeyDisplayText);
        _captureToClipboardItem.Text = $"截图到剪切板    {hotkeyDisplayText}";
    }

    public void ShowError(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        _notifyIcon.ShowBalloonTip(4000, "截图助手", message, ToolTipIcon.Error);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.DoubleClick -= NotifyIcon_DoubleClick;
        _iconBinding.Dispose();
        _notifyIcon.Dispose();
        _menu.Dispose();
    }

    private static ToolStripMenuItem CreateMenuItem(
        string text,
        EventHandler onClick)
    {
        ToolStripMenuItem item = new(text);
        item.Click += onClick;
        return item;
    }

    private static ToolStripMenuItem CreateToggleMenuItem(
        string text,
        bool isChecked,
        EventHandler onClick)
    {
        ToolStripMenuItem item = CreateMenuItem(text, onClick);
        item.CheckOnClick = true;
        item.Checked = isChecked;
        return item;
    }

    private void NotifyIcon_DoubleClick(object? sender, EventArgs eventArgs) =>
        ShowRequested?.Invoke();

    private void CloseToTrayItem_Click(object? sender, EventArgs eventArgs) =>
        CloseToTrayChanged?.Invoke(_closeToTrayItem.Checked);

    private void StartWithWindowsItem_Click(object? sender, EventArgs eventArgs) =>
        StartWithWindowsChanged?.Invoke(_startWithWindowsItem.Checked);

    private void ShowFloatingToolbarItem_Click(object? sender, EventArgs eventArgs) =>
        ShowFloatingToolbarChanged?.Invoke(_showFloatingToolbarItem.Checked);
}
