using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ShareX.HelpersLib;
using ShareX.ScreenCaptureLib;

namespace ScreenshotAssistant.Capture;

public static partial class ShareXCaptureDiagnosticsService
{
    private const uint DefaultDpi = 96;
    private const uint MonitorDefaultToNearest = 2;
    private const int EffectiveDpi = 0;
    private const int PerMonitorDpiAware = 2;

    public static CaptureDiagnosticsReport Run(int captureCount = 10)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(captureCount);
        _ = SetProcessDpiAwareness(PerMonitorDpiAware);

        DisplayDiagnostic[] displays = Screen.AllScreens
            .Select(CreateDisplayDiagnostic)
            .ToArray();
        Rectangle virtualBounds = CaptureHelpers.GetScreenBounds();
        Stopwatch stopwatch = Stopwatch.StartNew();
        int successfulCaptures = 0;
        Screenshot screenshot = new();

        for (int index = 0; index < captureCount; index++)
        {
            using Bitmap bitmap = screenshot.CaptureFullscreen();
            if (bitmap.Width == virtualBounds.Width &&
                bitmap.Height == virtualBounds.Height &&
                bitmap.Width > 0 &&
                bitmap.Height > 0)
            {
                _ = bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2);
                successfulCaptures++;
            }
        }

        stopwatch.Stop();
        string continuousCoverage = successfulCaptures == captureCount
            ? CaptureDiagnosticsEvaluator.Passed
            : "Failed";

        return new CaptureDiagnosticsReport(
            DateTimeOffset.UtcNow,
            displays,
            captureCount,
            successfulCaptures,
            virtualBounds.Width,
            virtualBounds.Height,
            stopwatch.Elapsed.TotalMilliseconds / captureCount,
            CaptureDiagnosticsEvaluator.EvaluateMultiMonitor(displays),
            CaptureDiagnosticsEvaluator.EvaluateMixedDpi(displays),
            continuousCoverage);
    }

    private static DisplayDiagnostic CreateDisplayDiagnostic(Screen screen)
    {
        Point center = new(
            screen.Bounds.Left + (screen.Bounds.Width / 2),
            screen.Bounds.Top + (screen.Bounds.Height / 2));
        IntPtr monitor = MonitorFromPoint(new NativePoint(center.X, center.Y), MonitorDefaultToNearest);
        uint dpiX = DefaultDpi;
        uint dpiY = DefaultDpi;
        if (monitor != IntPtr.Zero)
        {
            _ = GetDpiForMonitor(monitor, EffectiveDpi, out dpiX, out dpiY);
            dpiX = dpiX == 0 ? DefaultDpi : dpiX;
            dpiY = dpiY == 0 ? DefaultDpi : dpiY;
        }

        return new DisplayDiagnostic(
            screen.DeviceName,
            screen.Bounds.X,
            screen.Bounds.Y,
            screen.Bounds.Width,
            screen.Bounds.Height,
            dpiX,
            dpiY,
            screen.Primary);
    }

    [LibraryImport("user32.dll")]
    private static partial IntPtr MonitorFromPoint(NativePoint point, uint flags);

    [LibraryImport("shcore.dll")]
    private static partial int GetDpiForMonitor(
        IntPtr monitor,
        int dpiType,
        out uint dpiX,
        out uint dpiY);

    [LibraryImport("shcore.dll")]
    private static partial int SetProcessDpiAwareness(int awareness);

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct NativePoint(int X, int Y);
}
