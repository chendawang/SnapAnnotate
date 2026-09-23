using ScreenshotAssistant.Capture;

namespace ScreenshotAssistant.Capture.Tests;

public sealed class CaptureDiagnosticsEvaluatorTests
{
    [Fact]
    public void EvaluateCoverage_WithTwoDifferentDpiDisplays_IsCovered()
    {
        DisplayDiagnostic[] displays =
        [
            new("DISPLAY1", 0, 0, 1920, 1080, 96, 96, true),
            new("DISPLAY2", 1920, 0, 2560, 1440, 144, 144, false)
        ];

        Assert.Equal(
            CaptureDiagnosticsEvaluator.Passed,
            CaptureDiagnosticsEvaluator.EvaluateMultiMonitor(displays));
        Assert.Equal(
            CaptureDiagnosticsEvaluator.Passed,
            CaptureDiagnosticsEvaluator.EvaluateMixedDpi(displays));
    }

    [Fact]
    public void EvaluateCoverage_WithOneDisplay_IsNotCovered()
    {
        DisplayDiagnostic[] displays =
        [
            new("DISPLAY1", 0, 0, 1920, 1080, 96, 96, true)
        ];

        Assert.Equal(
            CaptureDiagnosticsEvaluator.NotCovered,
            CaptureDiagnosticsEvaluator.EvaluateMultiMonitor(displays));
        Assert.Equal(
            CaptureDiagnosticsEvaluator.NotCovered,
            CaptureDiagnosticsEvaluator.EvaluateMixedDpi(displays));
    }
}
