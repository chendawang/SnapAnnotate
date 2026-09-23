using System.Windows.Forms;
using ShareX.HelpersLib;

namespace ScreenshotAssistant.Capture;

public sealed class ShareXSettingsService
{
    private readonly string _settingsPath;

    private ShareXSettingsService(
        ScreenshotAssistantSettings settings,
        string settingsPath)
    {
        Current = settings;
        _settingsPath = settingsPath;
    }

    public ScreenshotAssistantSettings Current { get; }

    public string SettingsDirectory =>
        Path.GetDirectoryName(_settingsPath) ?? AppContext.BaseDirectory;

    public IReadOnlyList<string> HotkeyOptions { get; } =
    [
        "Ctrl + Shift + A",
        "Ctrl + Alt + A",
        "Print Screen"
    ];

    public int CurrentHotkeyIndex => GetHotkeyIndex(Current.CaptureHotkey);

    public static int GetHotkeyIndex(Keys hotkey) => hotkey switch
    {
        Keys.PrintScreen => 2,
        var value when value == (Keys.Control | Keys.Alt | Keys.A) => 1,
        _ => 0
    };

    public static ShareXSettingsService LoadDefault()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dataDirectory = Path.Combine(localAppData, "ScreenshotAssistant", "data");
        string backupDirectory = Path.Combine(dataDirectory, "backups");
        string settingsPath = Path.Combine(dataDirectory, "Settings.json");

        ScreenshotAssistantSettings settings = ScreenshotAssistantSettings.Load(
            settingsPath,
            backupDirectory);
        settings.BackupFolder = backupDirectory;
        settings.CreateBackup = true;
        settings.CreateWeeklyBackup = true;

        return new ShareXSettingsService(settings, settingsPath);
    }

    public bool Save() => Current.Save(_settingsPath);

    public bool TryApplyHotkey(
        int selectedIndex,
        ShareXGlobalHotkeyService hotkeyService)
    {
        Keys hotkey = GetHotkey(selectedIndex);

        if (!hotkeyService.TryUpdateHotkey(hotkey))
        {
            return false;
        }

        Current.CaptureHotkey = hotkey;
        return true;
    }

    public static bool TryCreateFileNamePreview(
        string pattern,
        out string preview,
        out string error)
    {
        preview = "";
        error = "";
        if (string.IsNullOrWhiteSpace(pattern))
        {
            error = "文件名模板不能为空。";
            return false;
        }

        if (pattern.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            error = "文件名模板包含 Windows 不允许的字符。";
            return false;
        }

        try
        {
            NameParser parser = new(NameParserType.FileName)
            {
                ImageWidth = 1920,
                ImageHeight = 1080
            };
            preview = parser.Parse(pattern.Trim()) + ".png";
            return true;
        }
        catch (Exception exception)
        {
            error = $"文件名模板无效：{exception.Message}";
            preview = "";
            return false;
        }
    }

    private static Keys GetHotkey(int selectedIndex) => selectedIndex switch
    {
        1 => Keys.Control | Keys.Alt | Keys.A,
        2 => Keys.PrintScreen,
        _ => Keys.Control | Keys.Shift | Keys.A
    };
}
