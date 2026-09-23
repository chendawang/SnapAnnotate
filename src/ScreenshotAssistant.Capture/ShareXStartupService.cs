using ShareX.HelpersLib;

namespace ScreenshotAssistant.Capture;

public sealed class ShareXStartupService
{
    private const string ShortcutName = "Screenshot Assistant";
    private const string BackgroundArgument = "--background";
    private readonly string _executablePath;

    public ShareXStartupService(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        _executablePath = executablePath;
    }

    public bool IsEnabled => ShortcutHelpers.CheckShortcut(
        Environment.SpecialFolder.Startup,
        ShortcutName,
        _executablePath);

    public bool TrySetEnabled(bool enabled)
    {
        if (enabled == IsEnabled)
        {
            return true;
        }

        return ShortcutHelpers.SetShortcut(
            enabled,
            Environment.SpecialFolder.Startup,
            ShortcutName,
            _executablePath,
            BackgroundArgument);
    }
}
