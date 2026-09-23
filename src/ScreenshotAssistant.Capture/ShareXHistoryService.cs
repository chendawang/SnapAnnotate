using ShareX.HelpersLib;
using ShareX.HistoryLib;

namespace ScreenshotAssistant.Capture;

public sealed class ShareXHistoryService : IDisposable
{
    private readonly HistoryManagerSQLite _historyManager;
    private readonly ImageHistorySettings _settings;
    private readonly HistoryWindowServices _services;
    private bool _disposed;

    private ShareXHistoryService(
        string databasePath,
        ImageHistorySettings settings)
    {
        _historyManager = new HistoryManagerSQLite(databasePath);
        _settings = settings;
        _services = new HistoryWindowServices
        {
            ShowImage = OpenFile
        };
    }

    public static ShareXHistoryService CreateDefault(ImageHistorySettings settings)
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string databasePath = Path.Combine(
            localAppData,
            "ScreenshotAssistant",
            "data",
            "History.db");
        return new ShareXHistoryService(databasePath, settings);
    }

    public void Record(string filePath, DateTimeOffset capturedAt, int width, int height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        HistoryItem item = new()
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            DateTime = capturedAt.LocalDateTime,
            Type = "Image",
            Host = "",
            URL = "",
            ThumbnailURL = "",
            DeletionURL = "",
            ShortenedURL = "",
            Tags = new Dictionary<string, string>
            {
                ["Source"] = "ScreenshotAssistant",
                ["Dimensions"] = $"{width}x{height}"
            }
        };

        if (!_historyManager.AppendHistoryItem(item))
        {
            throw new InvalidOperationException("PNG 已保存，但 ShareX 历史记录写入失败。");
        }
    }

    public void ShowImageHistory()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        HistoryIntegration.ShowImageHistoryWindow(_historyManager, _settings, _services);
    }

    public async Task<IReadOnlyList<LocalHistoryImage>> GetRecentImagesAsync(
        int maximumCount = 120)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumCount);

        List<HistoryItem> items = await _historyManager.GetHistoryItemsAsync();
        return items
            .Where(item =>
                string.Equals(item.Type, "Image", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(item.FilePath) &&
                File.Exists(item.FilePath))
            .OrderByDescending(item => item.DateTime)
            .Take(maximumCount)
            .Select(item => new LocalHistoryImage(
                item.Id,
                item.FilePath,
                item.FileName,
                item.DateTime))
            .ToArray();
    }

    public void OpenImage(string filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        OpenFile(filePath);
    }

    public bool CopyImageToClipboard(string filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return ClipboardHelpers.CopyImageFromFile(filePath);
    }

    public bool CopyFilesToClipboard(IEnumerable<string> filePaths)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(filePaths);
        string[] existingFiles = filePaths
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return existingFiles.Length > 0 && ClipboardHelpers.CopyFile(existingFiles);
    }

    public void OpenContainingFolder(string filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        string fullPath = Path.GetFullPath(filePath);
        string? folderPath = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new DirectoryNotFoundException("无法确定截图所在目录。");
        }

        ShareXShellService.OpenFolder(folderPath);
    }

    public bool Delete(LocalHistoryImage image, bool deleteFile = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(image);

        HistoryItem? item = _historyManager.GetHistoryItems()
            .FirstOrDefault(candidate => candidate.Id == image.Id);
        if (item is null)
        {
            return false;
        }

        if (deleteFile && File.Exists(image.FilePath))
        {
            File.Delete(image.FilePath);
        }

        _historyManager.Delete(item);
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _historyManager.Dispose();
    }

    private static void OpenFile(string filePath)
    {
        ShareXShellService.OpenFile(filePath);
    }
}
