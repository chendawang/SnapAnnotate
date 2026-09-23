namespace ScreenshotAssistant.Capture;

public static class HistoryPagination
{
    public const int PageSize = 20;

    public static int FirstPageCount(int totalCount) =>
        Math.Clamp(totalCount, 0, PageSize);

    public static int NextPageCount(int totalCount, int currentCount) =>
        Math.Min(Math.Max(0, totalCount), Math.Max(0, currentCount) + PageSize);
}
