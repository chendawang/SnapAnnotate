namespace ScreenshotAssistant.Capture;

public sealed record DisplayDiagnostic(
    string DeviceName,
    int X,
    int Y,
    int Width,
    int Height,
    uint DpiX,
    uint DpiY,
    bool IsPrimary);

public sealed record CaptureDiagnosticsReport(
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<DisplayDiagnostic> Displays,
    int RequestedCaptureCount,
    int SuccessfulCaptureCount,
    int VirtualDesktopWidth,
    int VirtualDesktopHeight,
    double AverageCaptureMilliseconds,
    string MultiMonitorCoverage,
    string MixedDpiCoverage,
    string ContinuousCaptureCoverage)
{
    public bool Passed =>
        SuccessfulCaptureCount == RequestedCaptureCount &&
        ContinuousCaptureCoverage == CaptureDiagnosticsEvaluator.Passed;
}

public static class CaptureDiagnosticsEvaluator
{
    public const string Passed = "Passed";
    public const string NotCovered = "NotCovered";

    public static string EvaluateMultiMonitor(IReadOnlyCollection<DisplayDiagnostic> displays) =>
        displays.Count > 1 ? Passed : NotCovered;

    public static string EvaluateMixedDpi(IReadOnlyCollection<DisplayDiagnostic> displays) =>
        displays.Select(display => (display.DpiX, display.DpiY)).Distinct().Count() > 1
            ? Passed
            : NotCovered;
}
