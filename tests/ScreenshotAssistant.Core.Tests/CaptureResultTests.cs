using ScreenshotAssistant.Core.Capture;

namespace ScreenshotAssistant.Core.Tests;

public sealed class CaptureResultTests
{
    [Fact]
    public void Cancelled_DoesNotExposeAFilePath()
    {
        CaptureResult result = CaptureResult.Cancelled(DateTimeOffset.Now);

        Assert.Equal(CaptureOutcome.Cancelled, result.Outcome);
        Assert.Null(result.FilePath);
    }

    [Fact]
    public void Completed_RejectsBlankFilePath()
    {
        Assert.Throws<ArgumentException>(() =>
            CaptureResult.Completed(DateTimeOffset.Now, " "));
    }

    [Fact]
    public void CopiedToClipboard_CompletesWithoutAFilePath()
    {
        CaptureResult result = CaptureResult.CopiedToClipboard(DateTimeOffset.Now);

        Assert.Equal(CaptureOutcome.Completed, result.Outcome);
        Assert.Null(result.FilePath);
    }
}
