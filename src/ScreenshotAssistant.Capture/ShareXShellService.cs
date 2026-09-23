using System.Diagnostics;

namespace ScreenshotAssistant.Capture;

public static class ShareXShellService
{
    public static void OpenFolder(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);
        string fullPath = NormalizePath(folderPath);
        Directory.CreateDirectory(fullPath);
        Process.Start(new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    public static void OpenFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        string fullPath = NormalizePath(filePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("截图文件不存在。", fullPath);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true
        });
    }

    public static void OpenUrl(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        if (!url.IsAbsoluteUri || url.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("只允许打开 HTTPS 地址。", nameof(url));
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = url.AbsoluteUri,
            UseShellExecute = true
        });
    }

    private static string NormalizePath(string path) =>
        Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
}
