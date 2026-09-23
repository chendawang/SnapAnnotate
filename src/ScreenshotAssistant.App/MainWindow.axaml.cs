using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ScreenshotAssistant.Capture;
using ScreenshotAssistant.Core.Capture;
using ShareX.AvaloniaUI;

namespace ScreenshotAssistant.App;

public sealed partial class MainWindow : Window
{
    private readonly CaptureSessionGate _captureSessionGate = new();
    private readonly List<Bitmap> _captureHistoryBitmaps = [];
    private readonly List<Bitmap> _fullHistoryBitmaps = [];
    private readonly HashSet<DateTime> _collapsedHistoryDates = [];
    private readonly HashSet<long> _selectedHistoryIds = [];
    private readonly DispatcherTimer _toastTimer;
    private readonly ShareXGlobalHotkeyService? _hotkeyService;
    private readonly ShareXHistoryService? _historyService;
    private readonly ShareXRegionCaptureWorkflow? _captureWorkflow;
    private readonly ShareXSettingsService? _settingsService;
    private SettingsPage? _settingsPage;
    private IReadOnlyList<LocalHistoryImage> _historyImages = [];
    private int _fullHistoryVisibleCount = HistoryPagination.PageSize;
    private bool _loadingMoreHistory;
    private bool _allowApplicationExit;

    public MainWindow()
    {
        InitializeComponent();
        _toastTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _toastTimer.Tick += ToastTimer_Tick;
    }

    public MainWindow(
        ShareXRegionCaptureWorkflow captureWorkflow,
        ShareXGlobalHotkeyService hotkeyService,
        ShareXHistoryService historyService,
        ShareXSettingsService settingsService,
        ShareXStartupService startupService,
        GitHubUpdateService updateService)
        : this()
    {
        _captureWorkflow = captureWorkflow;
        _hotkeyService = hotkeyService;
        _historyService = historyService;
        _settingsService = settingsService;
        _settingsPage = new SettingsPage(settingsService, hotkeyService, startupService, updateService);
        _settingsPage.SettingsSaved += SettingsPage_SettingsSaved;
        SettingsPageHost.Content = _settingsPage;
        _hotkeyService.CaptureRequested += HotkeyService_CaptureRequested;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;

        QuickCaptureShortcutText.Text = _hotkeyService.DisplayText;
        if (!_hotkeyService.IsRegistered)
        {
            StatusText.Text = $"{_hotkeyService.DisplayText} 已被其他程序占用，可点击卡片截图。";
        }
    }

    public event EventHandler? ApplicationExitRequested;

    public event EventHandler? SettingsChanged;

    public event EventHandler? FloatingToolbarRequested;

    public event EventHandler? CaptureStarting;

    public event EventHandler? CaptureFinished;

    public Task RunCaptureFromExternalAsync(ShareXCaptureAction action) =>
        RunCaptureAsync(action);

    public void ShowCapturePage()
    {
        SwitchPage(CapturePage, CaptureNavButton);
        _ = RefreshHistoryAsync();
    }

    public void ShowHistoryPage()
    {
        SwitchPage(HistoryPage, HistoryNavButton);
        _ = RefreshHistoryAsync();
    }

    public void ShowSettingsPage()
    {
        _settingsPage?.Reload();
        SwitchPage(SettingsPageHost, SettingsNavButton);
    }

    public void ReloadSettings() => _settingsPage?.Reload();

    public void AllowApplicationExit() => _allowApplicationExit = true;

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs eventArgs)
    {
        await RefreshHistoryAsync();
    }

    private async void CaptureToFile_Click(object? sender, RoutedEventArgs eventArgs)
    {
        await RunCaptureAsync(ShareXCaptureAction.RegionToFile);
    }

    private void ShowFloatingToolbar_Click(object? sender, RoutedEventArgs eventArgs) =>
        FloatingToolbarRequested?.Invoke(this, EventArgs.Empty);

    private async void HotkeyService_CaptureRequested(object? sender, EventArgs eventArgs)
    {
        await RunCaptureAsync(ShareXCaptureAction.RegionToFile);
    }

    private void ShowCapturePage_Click(object? sender, RoutedEventArgs eventArgs)
    {
        ShowCapturePage();
    }

    private void ShowHistoryPage_Click(object? sender, RoutedEventArgs eventArgs)
    {
        ShowHistoryPage();
    }

    private async void RefreshHistory_Click(object? sender, RoutedEventArgs eventArgs)
    {
        await RefreshHistoryAsync();
    }

    private void ShowSettingsPage_Click(object? sender, RoutedEventArgs eventArgs)
    {
        ShowSettingsPage();
    }

    private async Task RunCaptureAsync(ShareXCaptureAction action)
    {
        if (!_captureSessionGate.TryEnter(out IDisposable? captureLease))
        {
            StatusText.Text = "已有截图任务正在进行。";
            return;
        }

        using (captureLease)
        {
            if (_captureWorkflow is null)
            {
                StatusText.Text = "截图服务尚未初始化。";
                return;
            }

            StatusText.Text = action == ShareXCaptureAction.FullscreenToFile
                ? "正在捕获全部显示器……"
                : "正在冻结屏幕并等待选区……";

            CaptureStarting?.Invoke(this, EventArgs.Empty);
            try
            {
                CaptureResult result = await _captureWorkflow.CaptureAsync(this, action);
                if (result.Outcome == CaptureOutcome.Cancelled)
                {
                    StatusText.Text = "已取消截图，没有生成文件。";
                }
                else if (result.FilePath is null)
                {
                    StatusText.Text = "截图已复制到剪切板，可直接粘贴。";
                }
                else
                {
                    StatusText.Text = $"截图已保存：{result.FilePath}";
                    await RefreshHistoryAsync();
                }
            }
            catch (Exception exception)
            {
                StatusText.Text = $"截图失败：{exception.Message}";
            }
            finally
            {
                CaptureFinished?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private async Task RefreshHistoryAsync()
    {
        if (_historyService is null)
        {
            return;
        }

        try
        {
            IReadOnlyList<LocalHistoryImage> images =
                await _historyService.GetRecentImagesAsync(int.MaxValue);
            RenderHistory(images);
        }
        catch (Exception exception)
        {
            RenderHistoryError(HistoryGroupsPanel, exception.Message);
            RenderHistoryError(FullHistoryGroupsPanel, exception.Message);
        }
    }

    private void RenderHistory(IReadOnlyList<LocalHistoryImage> images)
    {
        HistoryGroupsPanel.Children.Clear();
        FullHistoryGroupsPanel.Children.Clear();
        DisposeHistoryBitmaps();
        _historyImages = images;
        _selectedHistoryIds.RemoveWhere(id => images.All(image => image.Id != id));
        _fullHistoryVisibleCount = HistoryPagination.FirstPageCount(images.Count);
        _collapsedHistoryDates.Clear();

        IEnumerable<LocalHistoryImage> today = images.Where(image =>
            image.CapturedAt.Date == DateTime.Today);
        RenderHistoryPanel(
            HistoryGroupsPanel,
            today,
            _captureHistoryBitmaps,
            "今天还没有截图记录。",
            allowDateCollapse: false);
        RenderHistoryPanel(
            FullHistoryGroupsPanel,
            images.Take(_fullHistoryVisibleCount),
            _fullHistoryBitmaps,
            "还没有本地截图，先从快捷截图开始。",
            allowDateCollapse: true);
        UpdateHistorySelectionActions();
    }

    private void RenderHistoryPanel(
        StackPanel panel,
        IEnumerable<LocalHistoryImage> source,
        List<Bitmap> bitmapStore,
        string emptyText,
        bool allowDateCollapse)
    {
        LocalHistoryImage[] images = source.ToArray();
        if (images.Length == 0)
        {
            panel.Children.Add(new Border
            {
                Padding = new Thickness(18, 28),
                Background = new SolidColorBrush(Color.Parse("#FFFFFF")),
                BorderBrush = new SolidColorBrush(Color.Parse("#E7E9ED")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.Parse("#777C85")),
                    Text = emptyText
                }
            });
            return;
        }

        foreach (IGrouping<DateTime, LocalHistoryImage> group in
                 images.GroupBy(image => image.CapturedAt.Date))
        {
            StackPanel dateSection = new()
            {
                Spacing = 10
            };
            TextBlock dateLabel = new()
            {
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                Text = FormatHistoryDate(group.Key)
            };

            WrapPanel thumbnails = new()
            {
                Width = 576,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            foreach (LocalHistoryImage item in group)
            {
                Border thumbnail = CreateHistoryThumbnail(item, bitmapStore);
                thumbnails.Children.Add(thumbnail);
            }

            if (allowDateCollapse)
            {
                bool isCollapsed = _collapsedHistoryDates.Contains(group.Key);
                thumbnails.IsVisible = !isCollapsed;
                PathIcon toggleIcon = new()
                {
                    Width = 15,
                    Height = 15,
                    Data = GetDateToggleIcon(isCollapsed)
                };
                Button toggleButton = new()
                {
                    Classes = { "historyAction" },
                    Width = 32,
                    Height = 28,
                    Content = toggleIcon
                };
                ToolTip.SetTip(toggleButton, isCollapsed ? "展开" : "收起");
                toggleButton.Click += (_, eventArgs) =>
                {
                    bool collapse = thumbnails.IsVisible;
                    thumbnails.IsVisible = !collapse;
                    toggleIcon.Data = GetDateToggleIcon(collapse);
                    ToolTip.SetTip(toggleButton, collapse ? "展开" : "收起");
                    if (collapse)
                    {
                        _collapsedHistoryDates.Add(group.Key);
                    }
                    else
                    {
                        _collapsedHistoryDates.Remove(group.Key);
                    }

                    eventArgs.Handled = true;
                };

                Grid dateHeader = new()
                {
                    Width = 576,
                    Margin = new Thickness(0, 0, 16, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    ColumnDefinitions = new ColumnDefinitions("*,Auto")
                };
                dateHeader.Children.Add(dateLabel);
                Grid.SetColumn(toggleButton, 1);
                dateHeader.Children.Add(toggleButton);
                dateSection.Children.Add(dateHeader);
            }
            else
            {
                dateSection.Children.Add(dateLabel);
            }

            dateSection.Children.Add(thumbnails);
            panel.Children.Add(dateSection);
        }
    }

    private Border CreateHistoryThumbnail(LocalHistoryImage item, List<Bitmap> bitmapStore)
    {
        using FileStream stream = File.OpenRead(item.FilePath);
        Bitmap bitmap = new(stream);
        bitmapStore.Add(bitmap);

        Image preview = new()
        {
            Width = 180,
            Height = 180,
            MinWidth = 180,
            MinHeight = 180,
            MaxWidth = 180,
            MaxHeight = 180,
            Source = bitmap,
            Stretch = Stretch.UniformToFill
        };

        Button previewButton = new()
        {
            Width = 180,
            Height = 180,
            MinWidth = 180,
            MinHeight = 180,
            MaxWidth = 180,
            MaxHeight = 180,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(7, 7, 0, 0),
            Content = new Border
            {
                ClipToBounds = true,
                CornerRadius = new CornerRadius(7, 7, 0, 0),
                Child = preview
            },
            Margin = new Thickness(0),
            Tag = item.FilePath
        };
        ToolTip.SetTip(previewButton, $"打开 {item.FileName}");
        previewButton.Click += HistoryThumbnail_Click;

        Button copyButton = CreateHistoryActionButton(
            "M8 7V5A2 2 0 0 1 10 3H19A2 2 0 0 1 21 5V14A2 2 0 0 1 19 16H17 M5 8H14A2 2 0 0 1 16 10V19A2 2 0 0 1 14 21H5A2 2 0 0 1 3 19V10A2 2 0 0 1 5 8Z",
            "复制到剪切板",
            item.FilePath);
        copyButton.Click += CopyHistoryImage_Click;

        Button folderButton = CreateHistoryActionButton(
            "M3 6A2 2 0 0 1 5 4H10L12 6H19A2 2 0 0 1 21 8V18A2 2 0 0 1 19 20H5A2 2 0 0 1 3 18Z",
            "打开文件夹",
            item.FilePath);
        folderButton.Click += OpenHistoryFolder_Click;

        Button deleteButton = CreateHistoryActionButton(
            "M6.4 5L12 10.6L17.6 5L19 6.4L13.4 12L19 17.6L17.6 19L12 13.4L6.4 19L5 17.6L10.6 12L5 6.4Z",
            "删除截图记录和本地文件",
            item);
        if (deleteButton.Content is PathIcon deleteIcon)
        {
            deleteIcon.Width = 18;
            deleteIcon.Height = 16;
        }
        deleteButton.Foreground = new SolidColorBrush(Color.Parse("#D14343"));
        deleteButton.Click += DeleteHistoryImage_Click;

        StackPanel actionButtons = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2
        };
        actionButtons.Children.Add(copyButton);
        actionButtons.Children.Add(folderButton);
        actionButtons.Children.Add(deleteButton);

        Border actionOverlay = new()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 6, 6),
            Padding = new Thickness(2),
            Background = new SolidColorBrush(Color.Parse("#EEFFFFFF")),
            CornerRadius = new CornerRadius(6),
            Child = actionButtons
        };

        CheckBox selectionCheckBox = new()
        {
            Classes = { "historySelector" },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(6),
            IsChecked = _selectedHistoryIds.Contains(item.Id),
            Tag = item.Id
        };
        ToolTip.SetTip(selectionCheckBox, "选择截图");
        selectionCheckBox.PointerPressed += HistorySelectionCheckBox_PointerPressed;
        selectionCheckBox.IsCheckedChanged += HistorySelectionCheckBox_IsCheckedChanged;

        Grid content = new();
        content.Children.Add(previewButton);
        content.Children.Add(actionOverlay);
        content.Children.Add(selectionCheckBox);

        return new Border
        {
            Width = 182,
            Height = 182,
            MinWidth = 182,
            MinHeight = 182,
            MaxWidth = 182,
            MaxHeight = 182,
            Margin = new Thickness(0, 0, 10, 10),
            Background = new SolidColorBrush(Color.Parse("#FFFFFF")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E5E5E5")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            ClipToBounds = true,
            Child = content,
            Tag = item.FilePath
        };
    }

    private static Button CreateHistoryActionButton(
        string iconData,
        string tooltip,
        object tag)
    {
        Button button = new()
        {
            Classes = { "historyAction" },
            Content = new PathIcon
            {
                Data = StreamGeometry.Parse(iconData),
                Width = 15,
                Height = 15
            },
            Tag = tag
        };
        ToolTip.SetTip(button, tooltip);
        return button;
    }

    private void HistoryThumbnail_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is Button { Tag: string filePath })
        {
            try
            {
                _historyService?.OpenImage(filePath);
            }
            catch (Exception exception)
            {
                StatusText.Text = $"无法打开截图：{exception.Message}";
            }
        }
    }

    private void CopyHistoryImage_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Button { Tag: string filePath } || _historyService is null)
        {
            return;
        }

        if (_historyService.CopyImageToClipboard(filePath))
        {
            ShowToast("截图已复制到剪切板");
        }
        else
        {
            StatusText.Text = "复制失败，请确认图片文件仍然存在。";
        }
    }

    private void HistorySelectionCheckBox_IsCheckedChanged(
        object? sender,
        RoutedEventArgs eventArgs)
    {
        if (sender is not CheckBox { Tag: long id } checkBox)
        {
            return;
        }

        if (checkBox.IsChecked == true)
        {
            _selectedHistoryIds.Add(id);
        }
        else
        {
            _selectedHistoryIds.Remove(id);
        }

        UpdateHistorySelectionActions();
    }

    private static void HistorySelectionCheckBox_PointerPressed(
        object? sender,
        PointerPressedEventArgs eventArgs)
    {
        if (sender is not CheckBox checkBox ||
            !eventArgs.GetCurrentPoint(checkBox).Properties.IsLeftButtonPressed)
        {
            return;
        }

        checkBox.IsChecked = checkBox.IsChecked != true;
        eventArgs.Handled = true;
    }

    private void CopySelectedHistory_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (_historyService is null)
        {
            return;
        }

        LocalHistoryImage[] selected = GetSelectedHistoryImages();
        if (selected.Length == 0)
        {
            return;
        }

        if (_historyService.CopyFilesToClipboard(selected.Select(image => image.FilePath)))
        {
            ClearHistorySelection();
            ShowToast($"已复制 {selected.Length} 张截图文件");
        }
        else
        {
            StatusText.Text = "复制失败，请确认图片文件仍然存在。";
        }
    }

    private async void DeleteSelectedHistory_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (_historyService is null)
        {
            return;
        }

        LocalHistoryImage[] selected = GetSelectedHistoryImages();
        if (selected.Length == 0)
        {
            return;
        }

        DialogResult confirmation = MessageBox.Show(
            this,
            $"确定删除选中的 {selected.Length} 张截图吗？\n对应的本地 PNG 文件也会被删除。",
            "批量删除截图",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        try
        {
            foreach (LocalHistoryImage image in selected)
            {
                _historyService.Delete(image);
                _selectedHistoryIds.Remove(image.Id);
            }

            await RefreshHistoryAsync();
            ShowToast($"已删除 {selected.Length} 张截图");
        }
        catch (Exception exception)
        {
            StatusText.Text = $"批量删除失败：{exception.Message}";
            await RefreshHistoryAsync();
        }
    }

    private LocalHistoryImage[] GetSelectedHistoryImages() =>
        _historyImages
            .Where(image => _selectedHistoryIds.Contains(image.Id))
            .ToArray();

    private void UpdateHistorySelectionActions()
    {
        bool hasSelection = _selectedHistoryIds.Count > 0;
        CaptureCopySelectedButton.IsEnabled = hasSelection;
        CaptureDeleteSelectedButton.IsEnabled = hasSelection;
        FullHistoryCopySelectedButton.IsEnabled = hasSelection;
        FullHistoryDeleteSelectedButton.IsEnabled = hasSelection;
    }

    private void ClearHistorySelection()
    {
        _selectedHistoryIds.Clear();
        foreach (CheckBox checkBox in this.GetVisualDescendants()
                     .OfType<CheckBox>()
                     .Where(checkBox => checkBox.Classes.Contains("historySelector"))
                     .ToArray())
        {
            checkBox.IsChecked = false;
        }

        UpdateHistorySelectionActions();
    }

    private void ShowToast(string message)
    {
        ToastText.Text = message;
        ToastBorder.IsVisible = true;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void ToastTimer_Tick(object? sender, EventArgs eventArgs)
    {
        _toastTimer.Stop();
        ToastBorder.IsVisible = false;
    }

    private void OpenHistoryFolder_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is Button { Tag: string filePath })
        {
            try
            {
                _historyService?.OpenContainingFolder(filePath);
            }
            catch (Exception exception)
            {
                StatusText.Text = $"无法打开截图位置：{exception.Message}";
            }
        }
    }

    private async void DeleteHistoryImage_Click(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Button { Tag: LocalHistoryImage image } || _historyService is null)
        {
            return;
        }

        DialogResult confirmation = MessageBox.Show(
            this,
            $"确定删除“{image.FileName}”吗？\n本地 PNG 文件也会被删除。",
            "删除截图",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        try
        {
            if (!_historyService.Delete(image))
            {
                StatusText.Text = "截图记录已不存在。";
                return;
            }

            await RefreshHistoryAsync();
            ShowToast("截图已删除");
        }
        catch (Exception exception)
        {
            StatusText.Text = $"删除失败：{exception.Message}";
        }
    }

    private void FullHistoryScrollViewer_ScrollChanged(object? sender, ScrollChangedEventArgs eventArgs)
    {
        if (_loadingMoreHistory || _fullHistoryVisibleCount >= _historyImages.Count)
        {
            return;
        }

        double remaining = FullHistoryScrollViewer.Extent.Height -
            FullHistoryScrollViewer.Viewport.Height -
            FullHistoryScrollViewer.Offset.Y;
        if (remaining > 220)
        {
            return;
        }

        _loadingMoreHistory = true;
        double currentOffset = FullHistoryScrollViewer.Offset.Y;
        _fullHistoryVisibleCount = HistoryPagination.NextPageCount(
            _historyImages.Count,
            _fullHistoryVisibleCount);
        DisposeBitmaps(_fullHistoryBitmaps);
        FullHistoryGroupsPanel.Children.Clear();
        RenderHistoryPanel(
            FullHistoryGroupsPanel,
            _historyImages.Take(_fullHistoryVisibleCount),
            _fullHistoryBitmaps,
            "还没有本地截图，先从快捷截图开始。",
            allowDateCollapse: true);
        Dispatcher.UIThread.Post(() =>
        {
            FullHistoryScrollViewer.Offset = new Vector(0, currentOffset);
            _loadingMoreHistory = false;
        }, DispatcherPriority.Background);
    }

    private void SettingsPage_SettingsSaved(object? sender, EventArgs eventArgs)
    {
        if (_hotkeyService is not null)
        {
            QuickCaptureShortcutText.Text = _hotkeyService.DisplayText;
        }

        StatusText.Text = "设置已保存，后续截图立即生效。";
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SwitchPage(Control page, Button selectedNavigation)
    {
        CapturePage.IsVisible = ReferenceEquals(page, CapturePage);
        HistoryPage.IsVisible = ReferenceEquals(page, HistoryPage);
        SettingsPageHost.IsVisible = ReferenceEquals(page, SettingsPageHost);

        CaptureNavButton.Classes.Set("selected", ReferenceEquals(selectedNavigation, CaptureNavButton));
        HistoryNavButton.Classes.Set("selected", ReferenceEquals(selectedNavigation, HistoryNavButton));
        SettingsNavButton.Classes.Set("selected", ReferenceEquals(selectedNavigation, SettingsNavButton));
    }

    private static void RenderHistoryError(StackPanel panel, string message)
    {
        panel.Children.Clear();
        panel.Children.Add(new TextBlock
        {
            Foreground = new SolidColorBrush(Color.Parse("#B04444")),
            Text = $"历史记录读取失败：{message}"
        });
    }

    private static string FormatHistoryDate(DateTime date)
    {
        DateTime today = DateTime.Today;
        string prefix = date == today
            ? "今天"
            : date == today.AddDays(-1)
                ? "昨天"
                : date.ToString("dddd", CultureInfo.CurrentCulture);
        return $"{prefix}  ·  {date:yyyy年M月d日}";
    }

    private static StreamGeometry GetDateToggleIcon(bool isCollapsed) =>
        StreamGeometry.Parse(isCollapsed
            ? "M7 10L12 15L17 10"
            : "M7 14L12 9L17 14");

    private void DisposeHistoryBitmaps()
    {
        DisposeBitmaps(_captureHistoryBitmaps);
        DisposeBitmaps(_fullHistoryBitmaps);
    }

    private static void DisposeBitmaps(List<Bitmap> bitmaps)
    {
        foreach (Bitmap bitmap in bitmaps)
        {
            bitmap.Dispose();
        }

        bitmaps.Clear();
    }

    private void MainWindow_Closing(object? sender, WindowClosingEventArgs eventArgs)
    {
        if (_allowApplicationExit)
        {
            return;
        }

        eventArgs.Cancel = true;
        if (_settingsService?.Current.CloseToTray == true)
        {
            Hide();
            StatusText.Text = "截图助手仍在托盘运行。";
        }
        else
        {
            ApplicationExitRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs eventArgs)
    {
        _toastTimer.Stop();
        _toastTimer.Tick -= ToastTimer_Tick;
        Loaded -= MainWindow_Loaded;
        Closing -= MainWindow_Closing;
        DisposeHistoryBitmaps();

        if (_hotkeyService is not null)
        {
            _hotkeyService.CaptureRequested -= HotkeyService_CaptureRequested;
        }

        if (_settingsPage is not null)
        {
            _settingsPage.SettingsSaved -= SettingsPage_SettingsSaved;
            _settingsPage = null;
        }
    }
}
