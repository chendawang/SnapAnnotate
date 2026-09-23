namespace ScreenshotAssistant.Capture;

public sealed record LocalHistoryImage(
    long Id,
    string FilePath,
    string FileName,
    DateTime CapturedAt);
