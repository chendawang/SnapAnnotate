using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using ScreenshotAssistant.Capture;

namespace ScreenshotAssistant.App;

public sealed partial class SettingsPage : UserControl
{
    private readonly ShareXSettingsService? _settingsService;
    private readonly ShareXGlobalHotkeyService? _hotkeyService;
    private readonly ShareXStartupService? _startupService;
    private readonly GitHubUpdateService? _updateService;
    private Uri? _latestReleasePage;

    public SettingsPage()
    {
        InitializeComponent();
    }

    public SettingsPage(
        ShareXSettingsService settingsService,
        ShareXGlobalHotkeyService hotkeyService,
        ShareXStartupService startupService,
        GitHubUpdateService updateService)
        : this()
    {
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        _startupService = startupService;
        _updateService = updateService;
        Reload();
    }

    public event EventHandler? SettingsSaved;

    private async void CheckUpdates_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (_latestReleasePage is not null)
        {
            ShareXShellService.OpenUrl(_latestReleasePage);
            return;
        }

        if (_updateService is null)
        {
            return;
        }

        CheckUpdatesButton.IsEnabled = false;
        SetStatus("正在从 GitHub 检查新版本……", "#356B9A");
        try
        {
            UpdateCheckResult result = await _updateService.CheckAsync();
            string color = result.Status switch
            {
                UpdateCheckStatus.Current => "#2E8B57",
                UpdateCheckStatus.UpdateAvailable => "#356B9A",
                _ => "#C2413B"
            };
            SetStatus(result.Message, color);
            if (result.ReleasePage is not null)
            {
                _latestReleasePage = result.ReleasePage;
                CheckUpdatesButton.Content = "打开下载页";
            }
        }
        finally
        {
            CheckUpdatesButton.IsEnabled = true;
        }
    }

    public void Reload()
    {
        if (_settingsService is null)
        {
            return;
        }

        HotkeyComboBox.ItemsSource = _settingsService.HotkeyOptions;
        ApplySettingsToControls(
            _settingsService.Current,
            _startupService?.IsEnabled ?? false);
        SetStatus("", "#777C85");
    }

    private void ApplySettingsToControls(
        ScreenshotAssistantSettings settings,
        bool startWithWindows)
    {
        HotkeyComboBox.SelectedIndex = ShareXSettingsService.GetHotkeyIndex(
            settings.CaptureHotkey);
        CloseToTrayCheckBox.IsChecked = settings.CloseToTray;
        StartWithWindowsCheckBox.IsChecked = startWithWindows;
        ShowFloatingToolbarCheckBox.IsChecked = settings.ShowFloatingToolbar;
        DetectWindowsCheckBox.IsChecked = settings.RegionCapture.DetectWindows;
        DetectControlsCheckBox.IsChecked = settings.RegionCapture.DetectControls;
        AllDisplaysRadio.IsChecked = !settings.RegionCapture.ActiveMonitorMode;
        ActiveMonitorRadio.IsChecked = settings.RegionCapture.ActiveMonitorMode;
        CaptureCursorCheckBox.IsChecked = settings.CaptureCursor;
        ShowMagnifierCheckBox.IsChecked = settings.RegionCapture.ShowMagnifier;
        SquareMagnifierCheckBox.IsChecked = settings.RegionCapture.UseSquareMagnifier;
        ShowInfoCheckBox.IsChecked = settings.RegionCapture.ShowInfo;
        ShowCenterCrosshairCheckBox.IsChecked = settings.RegionCapture.ShowCenterCrosshair;
        ShowScreenCrosshairCheckBox.IsChecked = settings.RegionCapture.ShowScreenCrosshair;
        BackgroundDimInput.Value = settings.RegionCapture.BackgroundDimStrength;
        SelectionFrameStyleComboBox.SelectedIndex = Math.Clamp(
            (int)settings.SelectionFrameLineStyle,
            0,
            2);
        SelectionFrameColorTextBox.Text = settings.SelectionFrameColor;
        SelectionFrameThicknessInput.Value = (decimal)settings.SelectionFrameThickness;
        EnableAnnotationsCheckBox.IsChecked = settings.EnableAnnotations;
        ThicknessInput.Value = settings.ImageEditor.Thickness;
        SaveDirectoryTextBox.Text = settings.SaveDirectory;
        OrganizeByMonthCheckBox.IsChecked = settings.OrganizeByMonth;
        FileNamePatternTextBox.Text = settings.FileNamePattern;
        UpdateFileNamePreview();
    }

    private async void BrowseDirectory_Click(object? sender, RoutedEventArgs eventArgs)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "选择截图保存目录",
                AllowMultiple = false
            });
        string? localPath = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            SaveDirectoryTextBox.Text = localPath;
        }
    }

    private void OpenSaveDirectory_Click(object? sender, RoutedEventArgs eventArgs)
    {
        string path = SaveDirectoryTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("请先填写保存目录。", "#C2413B");
            return;
        }

        try
        {
            ShareXShellService.OpenFolder(Path.GetFullPath(path));
        }
        catch (Exception exception)
        {
            SetStatus($"无法打开保存目录：{exception.Message}", "#C2413B");
        }
    }

    private void RestoreDefaults_Click(object? sender, RoutedEventArgs eventArgs)
    {
        ScreenshotAssistantSettings defaults = new();
        ApplySettingsToControls(defaults, false);
        SetStatus("已载入默认值，点击“保存设置”后生效。", "#356B9A");
    }

    private void FileNamePattern_TextChanged(object? sender, TextChangedEventArgs eventArgs) =>
        UpdateFileNamePreview();

    private void UpdateFileNamePreview()
    {
        string pattern = FileNamePatternTextBox.Text?.Trim() ?? "";
        if (ShareXSettingsService.TryCreateFileNamePreview(
                pattern,
                out string preview,
                out string error))
        {
            FileNamePreviewText.Foreground = new SolidColorBrush(Color.Parse("#4F657D"));
            FileNamePreviewText.Text = $"示例：{preview}";
        }
        else
        {
            FileNamePreviewText.Foreground = new SolidColorBrush(Color.Parse("#C2413B"));
            FileNamePreviewText.Text = error;
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (_settingsService is null || _hotkeyService is null || _startupService is null)
        {
            return;
        }

        string saveDirectory = SaveDirectoryTextBox.Text?.Trim() ?? "";
        string fileNamePattern = FileNamePatternTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(saveDirectory))
        {
            SetStatus("保存目录不能为空。", "#C2413B");
            return;
        }

        if (!ShareXSettingsService.TryCreateFileNamePreview(
                fileNamePattern,
                out _,
                out string patternError))
        {
            SetStatus(patternError, "#C2413B");
            return;
        }

        string selectionFrameColor = SelectionFrameColorTextBox.Text?.Trim() ?? "";
        if (!IsValidColor(selectionFrameColor))
        {
            SetStatus("截图框颜色无效，请输入例如 #338FF5。", "#C2413B");
            return;
        }

        try
        {
            saveDirectory = Path.GetFullPath(saveDirectory);
            Directory.CreateDirectory(saveDirectory);
        }
        catch (Exception exception)
        {
            SetStatus($"保存目录不可用：{exception.Message}", "#C2413B");
            return;
        }

        ScreenshotAssistantSettings settings = _settingsService.Current;
        EditableSettingsSnapshot previousSettings = EditableSettingsSnapshot.Capture(settings);
        var previousHotkey = _hotkeyService.Hotkey;
        if (!_settingsService.TryApplyHotkey(HotkeyComboBox.SelectedIndex, _hotkeyService))
        {
            SetStatus("这个快捷键已被其他程序占用。", "#C2413B");
            return;
        }

        bool previousStartupState = _startupService.IsEnabled;
        bool requestedStartupState = StartWithWindowsCheckBox.IsChecked == true;
        if (!_startupService.TrySetEnabled(requestedStartupState))
        {
            _hotkeyService.TryUpdateHotkey(previousHotkey);
            settings.CaptureHotkey = previousHotkey;
            SetStatus("开机启动设置失败，请检查启动目录权限。", "#C2413B");
            return;
        }

        settings.CloseToTray = CloseToTrayCheckBox.IsChecked == true;
        settings.StartWithWindows = requestedStartupState;
        settings.ShowFloatingToolbar = ShowFloatingToolbarCheckBox.IsChecked == true;
        settings.RegionCapture.DetectWindows = DetectWindowsCheckBox.IsChecked == true;
        settings.RegionCapture.DetectControls = DetectControlsCheckBox.IsChecked == true;
        settings.RegionCapture.ActiveMonitorMode = ActiveMonitorRadio.IsChecked == true;
        settings.RegionCapture.ShowMagnifier = ShowMagnifierCheckBox.IsChecked == true;
        settings.RegionCapture.UseSquareMagnifier = SquareMagnifierCheckBox.IsChecked == true;
        settings.RegionCapture.ShowInfo = ShowInfoCheckBox.IsChecked == true;
        settings.RegionCapture.ShowCenterCrosshair = ShowCenterCrosshairCheckBox.IsChecked == true;
        settings.RegionCapture.ShowScreenCrosshair = ShowScreenCrosshairCheckBox.IsChecked == true;
        settings.RegionCapture.BackgroundDimStrength = decimal.ToInt32(BackgroundDimInput.Value ?? 20);
        settings.CaptureCursor = CaptureCursorCheckBox.IsChecked == true;
        settings.HotkeyAction = ShareXCaptureAction.RegionToFile;
        settings.SelectionFrameLineStyle = (SelectionFrameLineStyle)Math.Clamp(
            SelectionFrameStyleComboBox.SelectedIndex,
            0,
            2);
        settings.SelectionFrameColor = selectionFrameColor;
        settings.SelectionFrameThickness = decimal.ToDouble(
            SelectionFrameThicknessInput.Value ?? 2);
        settings.EnableAnnotations = EnableAnnotationsCheckBox.IsChecked == true;
        settings.ImageEditor.Thickness = decimal.ToInt32(ThicknessInput.Value ?? 4);
        settings.SaveDirectory = saveDirectory;
        settings.OrganizeByMonth = OrganizeByMonthCheckBox.IsChecked == true;
        settings.FileNamePattern = fileNamePattern;

        if (!_settingsService.Save())
        {
            _startupService.TrySetEnabled(previousStartupState);
            _hotkeyService.TryUpdateHotkey(previousHotkey);
            settings.CaptureHotkey = previousHotkey;
            previousSettings.Restore(settings);
            SetStatus("设置保存失败，已恢复保存前的配置。", "#C2413B");
            return;
        }

        SaveDirectoryTextBox.Text = saveDirectory;
        SetStatus("设置已保存，后续截图立即生效。", "#2E8B57");
        SettingsSaved?.Invoke(this, EventArgs.Empty);
    }

    private void SetStatus(string message, string color)
    {
        SettingsStatusText.Foreground = new SolidColorBrush(Color.Parse(color));
        SettingsStatusText.Text = message;
    }

    private static bool IsValidColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            _ = Color.Parse(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private readonly record struct EditableSettingsSnapshot(
        bool CloseToTray,
        bool StartWithWindows,
        bool ShowFloatingToolbar,
        bool DetectWindows,
        bool DetectControls,
        bool ActiveMonitorMode,
        bool ShowMagnifier,
        bool SquareMagnifier,
        bool ShowInfo,
        bool ShowCenterCrosshair,
        bool ShowScreenCrosshair,
        int BackgroundDimStrength,
        bool CaptureCursor,
        ShareXCaptureAction HotkeyAction,
        SelectionFrameLineStyle SelectionFrameLineStyle,
        string SelectionFrameColor,
        double SelectionFrameThickness,
        bool EnableAnnotations,
        int Thickness,
        string SaveDirectory,
        bool OrganizeByMonth,
        string FileNamePattern)
    {
        public static EditableSettingsSnapshot Capture(ScreenshotAssistantSettings settings) =>
            new(
                settings.CloseToTray,
                settings.StartWithWindows,
                settings.ShowFloatingToolbar,
                settings.RegionCapture.DetectWindows,
                settings.RegionCapture.DetectControls,
                settings.RegionCapture.ActiveMonitorMode,
                settings.RegionCapture.ShowMagnifier,
                settings.RegionCapture.UseSquareMagnifier,
                settings.RegionCapture.ShowInfo,
                settings.RegionCapture.ShowCenterCrosshair,
                settings.RegionCapture.ShowScreenCrosshair,
                settings.RegionCapture.BackgroundDimStrength,
                settings.CaptureCursor,
                settings.HotkeyAction,
                settings.SelectionFrameLineStyle,
                settings.SelectionFrameColor,
                settings.SelectionFrameThickness,
                settings.EnableAnnotations,
                settings.ImageEditor.Thickness,
                settings.SaveDirectory,
                settings.OrganizeByMonth,
                settings.FileNamePattern);

        public void Restore(ScreenshotAssistantSettings settings)
        {
            settings.CloseToTray = CloseToTray;
            settings.StartWithWindows = StartWithWindows;
            settings.ShowFloatingToolbar = ShowFloatingToolbar;
            settings.RegionCapture.DetectWindows = DetectWindows;
            settings.RegionCapture.DetectControls = DetectControls;
            settings.RegionCapture.ActiveMonitorMode = ActiveMonitorMode;
            settings.RegionCapture.ShowMagnifier = ShowMagnifier;
            settings.RegionCapture.UseSquareMagnifier = SquareMagnifier;
            settings.RegionCapture.ShowInfo = ShowInfo;
            settings.RegionCapture.ShowCenterCrosshair = ShowCenterCrosshair;
            settings.RegionCapture.ShowScreenCrosshair = ShowScreenCrosshair;
            settings.RegionCapture.BackgroundDimStrength = BackgroundDimStrength;
            settings.CaptureCursor = CaptureCursor;
            settings.HotkeyAction = HotkeyAction;
            settings.SelectionFrameLineStyle = SelectionFrameLineStyle;
            settings.SelectionFrameColor = SelectionFrameColor;
            settings.SelectionFrameThickness = SelectionFrameThickness;
            settings.EnableAnnotations = EnableAnnotations;
            settings.ImageEditor.Thickness = Thickness;
            settings.SaveDirectory = SaveDirectory;
            settings.OrganizeByMonth = OrganizeByMonth;
            settings.FileNamePattern = FileNamePattern;
        }
    }
}
