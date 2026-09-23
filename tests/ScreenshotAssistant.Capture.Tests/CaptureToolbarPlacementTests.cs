using Avalonia;
using ScreenshotAssistant.Capture;

namespace ScreenshotAssistant.Capture.Tests;

public sealed class CaptureToolbarPlacementTests
{
    [Fact]
    public void Calculate_CentersToolbarBelowSelection()
    {
        Point result = CaptureToolbarPlacement.Calculate(
            new Rect(200, 100, 400, 240),
            new Size(300, 48),
            new Size(1000, 700));

        Assert.Equal(new Point(250, 348), result);
    }

    [Fact]
    public void Calculate_MovesToolbarAboveWhenBottomHasNoRoom()
    {
        Point result = CaptureToolbarPlacement.Calculate(
            new Rect(200, 600, 400, 80),
            new Size(300, 48),
            new Size(1000, 700));

        Assert.Equal(new Point(250, 544), result);
    }

    [Fact]
    public void Calculate_KeepsToolbarInsideHorizontalMargins()
    {
        Point result = CaptureToolbarPlacement.Calculate(
            new Rect(0, 100, 80, 80),
            new Size(300, 48),
            new Size(1000, 700));

        Assert.Equal(8, result.X);
    }

    [Fact]
    public void ToLogical_ConvertsPhysicalSelectionUsingWindowScale()
    {
        Rect result = CaptureToolbarPlacement.ToLogical(
            new Rect(300, 150, 600, 450),
            1.5);

        Assert.Equal(new Rect(200, 100, 400, 300), result);
    }
}
