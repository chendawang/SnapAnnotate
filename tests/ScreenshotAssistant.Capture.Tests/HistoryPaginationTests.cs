using ScreenshotAssistant.Capture;

namespace ScreenshotAssistant.Capture.Tests;

public sealed class HistoryPaginationTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(12, 12)]
    [InlineData(100, 20)]
    public void FirstPageCount_LoadsAtMostTwenty(int total, int expected)
    {
        Assert.Equal(expected, HistoryPagination.FirstPageCount(total));
    }

    [Theory]
    [InlineData(100, 20, 40)]
    [InlineData(45, 40, 45)]
    [InlineData(12, 12, 12)]
    public void NextPageCount_LoadsTwentyUntilEnd(int total, int current, int expected)
    {
        Assert.Equal(expected, HistoryPagination.NextPageCount(total, current));
    }
}
