using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace ScreenshotAssistant.Capture;

internal sealed class CaptureOwnerVisibility(Window owner) : IDisposable
{
    private const int SwHide = 0;
    private readonly bool wasVisible = owner.IsVisible;
    private readonly bool wasShownInTaskbar = owner.ShowInTaskbar;
    private bool disposed;

    public async Task HideAsync(CancellationToken cancellationToken)
    {
        if (!wasVisible)
        {
            return;
        }

        owner.ShowInTaskbar = false;
        owner.Hide();

        IntPtr handle = owner.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle != IntPtr.Zero)
        {
            _ = ShowWindow(handle, SwHide);
            _ = DwmFlush();
        }

        await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        owner.ShowInTaskbar = wasShownInTaskbar;
        if (wasVisible)
        {
            owner.Show();
            owner.Activate();
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr windowHandle, int command);

    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();
}
