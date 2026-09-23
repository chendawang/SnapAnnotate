using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ShareX.AvaloniaUI.Theming;
using ShareX.ImageEditor.Core.Abstractions;
using ShareX.ImageEditor.Core.Annotations;
using ShareX.ImageEditor.Integration;
using ShareX.ImageEditor.Presentation.Controls;
using ShareX.ImageEditor.Presentation.Theming;
using ShareX.ImageEditor.Presentation.ViewModels;
using ShareX.ImageEditor.Presentation.Views;
using ShareX.ScreenCaptureLib.Presentation.RegionCapture;

namespace ScreenshotAssistant.Capture;

internal sealed record ShareXRegionCaptureSessionResult(
    AvaloniaRegionCaptureResult? Capture,
    ShareXCaptureAction Action);

internal sealed record SelectionFrameAppearance(
    string Color,
    double Thickness,
    SelectionFrameLineStyle LineStyle);

internal sealed class ShareXRegionCaptureSession
{
    private static readonly MethodInfo CompleteMethod = typeof(RegionCaptureWindow)
        .GetMethod("Complete", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(typeof(RegionCaptureWindow).FullName, "Complete");

    private static readonly MethodInfo CancelMethod = typeof(RegionCaptureWindow)
        .GetMethod("CancelCapture", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(typeof(RegionCaptureWindow).FullName, "CancelCapture");

    private readonly RegionCaptureWindow window;
    private readonly Grid captureToolbar;
    private readonly AnnotationToolbar annotationToolbar;
    private readonly RegionSelectionOverlay regionOverlay;
    private readonly Grid regionInputSurface;
    private readonly Canvas regionResizeNodeCanvas;
    private readonly Button regionToolButton;
    private readonly StackPanel magnifierPanel;
    private readonly Canvas inputShieldCanvas;
    private readonly Border[] outsideShields;
    private readonly Canvas annotationCanvas;
    private readonly DispatcherTimer placementTimer;
    private readonly CaptureInteractionState interactionState = new();
    private bool actionButtonsAppended;
    private ShareXCaptureAction selectedAction;

    private ShareXRegionCaptureSession(
        AvaloniaRegionCaptureRequest request,
        ShareXCaptureAction initialAction,
        SelectionFrameAppearance selectionFrame)
    {
        selectedAction = initialAction;
        ConfigureVisibleTools(request.ImageEditorOptions);
        ImageEditorIntegration.Initialize();

        window = new RegionCaptureWindow(request);
        captureToolbar = window.FindControl<Grid>("CaptureToolbar")!;
        annotationToolbar = window.FindControl<AnnotationToolbar>("CaptureAnnotationToolbar")!;
        regionOverlay = window.FindControl<RegionSelectionOverlay>("RegionOverlay")!;
        regionInputSurface = window.FindControl<Grid>("RegionInputSurface")!;
        regionResizeNodeCanvas = window.FindControl<Canvas>("RegionResizeNodeCanvas")!;
        regionToolButton = window.FindControl<Button>("RegionToolButton")!;
        magnifierPanel = window.FindControl<StackPanel>("MagnifierPanel")!;
        regionOverlay.AccentBrush = new SolidColorBrush(ParseSelectionColor(selectionFrame.Color));
        regionOverlay.SelectionBorderThickness = Math.Clamp(selectionFrame.Thickness, 1, 8);
        regionOverlay.SelectionBorderDashStyle = selectionFrame.LineStyle switch
        {
            SelectionFrameLineStyle.Dashed => new DashStyle([6, 4], 0),
            SelectionFrameLineStyle.Dotted => new DashStyle([1, 3], 0),
            _ => null
        };
        EditorView editorWorkspace = window.FindControl<EditorView>("EditorWorkspace")!;
        if (editorWorkspace.DataContext is MainViewModel editorViewModel)
        {
            editorViewModel.SetHostToolbarFilter(item =>
                item.Tool is EditorTool tool && CaptureToolbarToolLayout.Primary.Contains(tool));
        }

        annotationCanvas = editorWorkspace.FindControl<Canvas>("AnnotationCanvas")!;

        Grid rootGrid = window.FindControl<Grid>("RootGrid")!;
        inputShieldCanvas = new Canvas { IsVisible = false };
        outsideShields =
        [
            CreateInputShield(),
            CreateInputShield(),
            CreateInputShield(),
            CreateInputShield()
        ];
        foreach (Border shield in outsideShields)
        {
            inputShieldCanvas.Children.Add(shield);
        }

        rootGrid.Children.Insert(Math.Max(0, rootGrid.Children.Count - 1), inputShieldCanvas);

        annotationToolbar.MainToolbarCornerRadius = new CornerRadius(10);
        annotationToolbar.MainToolbarBorderThickness = new Thickness(1);
        annotationToolbar.ShowEditingActions = false;
        captureToolbar.IsVisible = true;
        captureToolbar.Opacity = 0;
        captureToolbar.IsHitTestVisible = false;

        placementTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(33), DispatcherPriority.Render, (_, _) => UpdateToolbarPlacement());

        window.Opened += OnOpened;
        window.Closed += (_, _) => placementTimer.Stop();
        window.AddHandler(
            InputElement.PointerReleasedEvent,
            OnWindowPointerReleased,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
        captureToolbar.LayoutUpdated += OnToolbarLayoutUpdated;
        regionOverlay.AddHandler(
            InputElement.PointerMovedEvent,
            OnSelectionPointerChanged,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
        regionOverlay.AddHandler(
            InputElement.PointerReleasedEvent,
            OnSelectionPointerChanged,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    public static async Task<ShareXRegionCaptureSessionResult> CaptureAsync(
        AvaloniaRegionCaptureRequest request,
        ShareXCaptureAction initialAction,
        SelectionFrameAppearance selectionFrame)
    {
        ShareXRegionCaptureSession session = new(request, initialAction, selectionFrame);
        AvaloniaRegionCaptureResult? result = await session.window.CaptureAsync();
        return new ShareXRegionCaptureSessionResult(result, session.selectedAction);
    }

    private static void ConfigureVisibleTools(ImageEditorOptions options)
    {
        HashSet<string> visibleIds = CaptureToolbarToolLayout.Primary
            .Select(tool => tool.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        options.ToolbarItems = ToolbarCustomizationItemViewModel.CreateDefaultItems()
            .Select(item => new ImageEditorToolbarItemOptions
            {
                Id = item.Id,
                BeginGroup = false,
                Hotkey = item.Hotkey,
                IsVisible = visibleIds.Contains(item.Id)
            })
            .ToList();
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        TryAppendActionButtons();
        placementTimer.Start();
        Dispatcher.UIThread.Post(UpdateToolbarPlacement, DispatcherPriority.Loaded);
    }

    private void TryAppendActionButtons()
    {
        if (actionButtonsAppended)
        {
            return;
        }

        StackPanel? mainRow = annotationToolbar.GetVisualDescendants()
            .OfType<StackPanel>()
            .FirstOrDefault(panel =>
                panel.Orientation == Orientation.Horizontal &&
                panel.GetVisualChildren().OfType<ItemsControl>().Any());
        IAnnotationToolbarAdapter? adapter = annotationToolbar.DataContext as IAnnotationToolbarAdapter;
        if (mainRow == null || adapter == null)
        {
            return;
        }

        StackPanel actions = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4
        };
        actions.Children.Add(CreateDivider());
        actions.Children.Add(CreateButton(EditorIcons.ActionUndo, "撤销  Ctrl+Z", (_, _) => adapter.Undo()));
        actions.Children.Add(CreateButton(EditorIcons.ActionRedo, "重做  Ctrl+Y", (_, _) => adapter.Redo()));
        actions.Children.Add(CreateDivider());

        actions.Children.Add(CreateButton(
            LucideIcons.clipboard,
            "复制到剪贴板",
            (_, _) => Complete(ShareXCaptureAction.RegionToClipboard)));
        actions.Children.Add(CreateButton(
            LucideIcons.check,
            "保存为 PNG",
            (_, _) => Complete(ShareXCaptureAction.RegionToFile),
            new SolidColorBrush(Color.Parse("#1AAD19"))));
        actions.Children.Add(CreateButton(
            LucideIcons.x,
            "取消截图  Esc",
            (_, _) => Cancel(),
            new SolidColorBrush(Color.Parse("#D14343"))));
        mainRow.Children.Add(CreateMoreButton(adapter));
        mainRow.Children.Add(actions);
        actionButtonsAppended = true;
    }

    private static Border CreateDivider() => new()
    {
        Width = 1,
        Margin = new Thickness(2, 4),
        Background = new SolidColorBrush(Color.Parse("#E5E7EB"))
    };

    private static Button CreateButton(
        string icon,
        string tooltip,
        EventHandler<RoutedEventArgs> click,
        IBrush? foreground = null)
    {
        Button button = new()
        {
            Content = icon,
            MinWidth = 36,
            Width = 36,
            Height = 36
        };
        if (foreground is not null)
        {
            button.Foreground = foreground;
        }

        button.Classes.Add("toolbar-button");
        ToolTip.SetTip(button, tooltip);
        button.Click += click;
        return button;
    }

    private static Button CreateMoreButton(IAnnotationToolbarAdapter adapter)
    {
        Button button = CreateButton(EditorIcons.ChevronDown, "更多标注工具", (_, _) => { });
        MenuFlyout flyout = new()
        {
            Placement = PlacementMode.BottomEdgeAlignedRight,
            VerticalOffset = 4
        };

        foreach (EditorTool tool in CaptureToolbarToolLayout.More)
        {
            MenuItem item = new()
            {
                Header = GetToolName(tool),
                Icon = new TextBlock
                {
                    Text = EditorIcons.ForTool(tool),
                    FontFamily = button.FontFamily
                }
            };
            item.Click += (_, _) => adapter.SelectTool(tool);
            flyout.Items.Add(item);
        }

        button.Flyout = flyout;
        return button;
    }

    private static string GetToolName(EditorTool tool) => tool switch
    {
        EditorTool.Line => "直线",
        EditorTool.Highlight => "高亮",
        EditorTool.Blur => "模糊",
        EditorTool.SmartEraser => "智能擦除",
        EditorTool.Magnify => "放大镜",
        EditorTool.Spotlight => "聚光灯",
        EditorTool.SpeechBalloon => "气泡文字",
        EditorTool.Image => "插入图片",
        EditorTool.Emoji => "表情",
        EditorTool.Cursor => "鼠标指针",
        _ => tool.ToString()
    };

    private void Complete(ShareXCaptureAction action)
    {
        Rect selection = regionOverlay.SelectionRectangle;
        if (!IsValidSelection(selection))
        {
            return;
        }

        selectedAction = action;
        CompleteMethod.Invoke(window, [selection, true]);
    }

    private void Cancel() => CancelMethod.Invoke(window, null);

    private void OnSelectionPointerChanged(object? sender, PointerEventArgs e) =>
        Dispatcher.UIThread.Post(UpdateToolbarPlacement, DispatcherPriority.Render);

    private void OnWindowPointerReleased(object? sender, PointerReleasedEventArgs e) =>
        Dispatcher.UIThread.Post(TryConfirmSelection, DispatcherPriority.Input);

    private void OnToolbarLayoutUpdated(object? sender, EventArgs e) => UpdateToolbarPlacement();

    private void UpdateToolbarPlacement()
    {
        TryAppendActionButtons();

        Rect selection = regionOverlay.SelectionRectangle;
        if (!interactionState.IsSelectionConfirmed || !IsValidSelection(selection))
        {
            captureToolbar.Opacity = 0;
            captureToolbar.IsHitTestVisible = false;
            return;
        }

        MaintainConfirmedSelection();

        Size toolbarSize = captureToolbar.Bounds.Size;
        if (toolbarSize.Width <= 0 || toolbarSize.Height <= 0)
        {
            toolbarSize = captureToolbar.DesiredSize;
        }

        if (toolbarSize.Width <= 0 || toolbarSize.Height <= 0)
        {
            return;
        }

        Rect logicalSelection = CaptureToolbarPlacement.ToLogical(selection, window.RenderScaling);
        Point position = CaptureToolbarPlacement.Calculate(
            logicalSelection,
            toolbarSize,
            window.Bounds.Size);
        Canvas.SetLeft(captureToolbar, position.X);
        Canvas.SetTop(captureToolbar, position.Y);
        captureToolbar.Opacity = 1;
        captureToolbar.IsHitTestVisible = true;
    }

    private void TryConfirmSelection()
    {
        if (interactionState.TryConfirm(regionOverlay.SelectionRectangle))
        {
            MaintainConfirmedSelection();
            UpdateToolbarPlacement();
        }
    }

    private void MaintainConfirmedSelection()
    {
        if (!interactionState.IsSelectionConfirmed)
        {
            return;
        }

        bool regionToolActive = regionToolButton.Classes.Contains("active");
        regionToolButton.IsVisible = true;
        magnifierPanel.IsVisible = false;
        regionInputSurface.IsVisible = regionToolActive;
        regionInputSurface.IsHitTestVisible = regionToolActive;
        regionOverlay.IsVisible = regionToolActive;
        regionResizeNodeCanvas.IsVisible = regionToolActive;
        annotationCanvas.Clip = new RectangleGeometry(regionOverlay.SelectionRectangle);
        if (regionToolActive)
        {
            inputShieldCanvas.IsVisible = false;
        }
        else
        {
            UpdateInputShields();
        }
    }

    private void UpdateInputShields()
    {
        Rect selection = CaptureToolbarPlacement.ToLogical(
            regionOverlay.SelectionRectangle,
            window.RenderScaling);
        Size surface = window.Bounds.Size;
        if (!IsValidSelection(selection) || surface.Width <= 0 || surface.Height <= 0)
        {
            inputShieldCanvas.IsVisible = false;
            return;
        }

        SetShieldBounds(outsideShields[0], new Rect(0, 0, surface.Width, selection.Top));
        SetShieldBounds(outsideShields[1], new Rect(
            0,
            selection.Bottom,
            surface.Width,
            Math.Max(0, surface.Height - selection.Bottom)));
        SetShieldBounds(outsideShields[2], new Rect(0, selection.Top, selection.Left, selection.Height));
        SetShieldBounds(outsideShields[3], new Rect(
            selection.Right,
            selection.Top,
            Math.Max(0, surface.Width - selection.Right),
            selection.Height));
        inputShieldCanvas.IsVisible = true;
    }

    private static Border CreateInputShield() => new()
    {
        Background = Brushes.Transparent
    };

    private static void SetShieldBounds(Control shield, Rect bounds)
    {
        Canvas.SetLeft(shield, bounds.X);
        Canvas.SetTop(shield, bounds.Y);
        shield.Width = Math.Max(0, bounds.Width);
        shield.Height = Math.Max(0, bounds.Height);
        shield.IsVisible = bounds.Width > 0 && bounds.Height > 0;
    }

    private static bool IsValidSelection(Rect selection) =>
        selection.Width > 0 && selection.Height > 0;

    private static Color ParseSelectionColor(string value)
    {
        try
        {
            return Color.Parse(value);
        }
        catch (FormatException)
        {
            return Color.Parse("#338FF5");
        }
    }
}
