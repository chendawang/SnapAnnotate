using Avalonia;
using ScreenshotAssistant.Capture;

namespace ScreenshotAssistant.Capture.Tests;

public sealed class CaptureInteractionStateTests
{
    [Fact]
    public void TryConfirm_LocksFirstValidSelection()
    {
        CaptureInteractionState state = new();

        Assert.True(state.TryConfirm(new Rect(10, 20, 300, 200)));
        Assert.True(state.IsSelectionConfirmed);
        Assert.False(state.TryConfirm(new Rect(30, 40, 100, 80)));
    }
}
