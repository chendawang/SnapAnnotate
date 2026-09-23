using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;

namespace ScreenshotAssistant.App;

public sealed partial class FloatingToolbarWindow : Window
{
    private bool _positioned;

    public FloatingToolbarWindow()
    {
        InitializeComponent();
        Opened += FloatingToolbarWindow_Opened;
    }

    public event EventHandler? CaptureRequested;

    public event EventHandler? MainWindowRequested;

    public event EventHandler? CloseRequested;

    private void Capture_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) =>
        CaptureRequested?.Invoke(this, EventArgs.Empty);

    private void OpenMainWindow_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) =>
        MainWindowRequested?.Invoke(this, EventArgs.Empty);

    private void CloseToolbar_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) =>
        CloseRequested?.Invoke(this, EventArgs.Empty);

    private void DragHandle_PointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (!eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        BeginMoveDrag(eventArgs);
    }

    private void FloatingToolbarWindow_Opened(object? sender, EventArgs eventArgs)
    {
        if (_positioned)
        {
            return;
        }

        Screen? screen = Screens.Primary;
        if (screen is null)
        {
            return;
        }

        PixelSize size = PixelSize.FromSize(ClientSize, screen.Scaling);
        Position = new PixelPoint(
            screen.WorkingArea.Right - size.Width - 24,
            screen.WorkingArea.Bottom - size.Height - 72);
        _positioned = true;
    }
}
