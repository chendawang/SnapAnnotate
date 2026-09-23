namespace ScreenshotAssistant.Core.Capture;

public sealed record CaptureResult
{
    private CaptureResult(
        CaptureOutcome outcome,
        DateTimeOffset completedAtUtc,
        string? filePath)
    {
        Outcome = outcome;
        CompletedAtUtc = completedAtUtc.ToUniversalTime();
        FilePath = filePath;
    }

    public CaptureOutcome Outcome { get; }

    public DateTimeOffset CompletedAtUtc { get; }

    public string? FilePath { get; }

    public static CaptureResult Cancelled(DateTimeOffset completedAt) =>
        new(CaptureOutcome.Cancelled, completedAt, null);

    public static CaptureResult Completed(DateTimeOffset completedAt, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return new CaptureResult(CaptureOutcome.Completed, completedAt, filePath);
    }

    public static CaptureResult CopiedToClipboard(DateTimeOffset completedAt) =>
        new(CaptureOutcome.Completed, completedAt, null);
}
