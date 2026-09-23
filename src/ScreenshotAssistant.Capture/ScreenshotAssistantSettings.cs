using System.Windows.Forms;
using ShareX.HelpersLib;
using ShareX.HistoryLib;
using ShareX.ImageEditor.Integration;
using ShareX.ScreenCaptureLib;

namespace ScreenshotAssistant.Capture;

public sealed class ScreenshotAssistantSettings : SettingsBase<ScreenshotAssistantSettings>
{
    public RegionCaptureOptions RegionCapture { get; set; } = new()
    {
        QuickCapture = false
    };

    public ImageEditorOptions ImageEditor { get; set; } = new();

    public ImageHistorySettings ImageHistory { get; set; } = new();

    public Keys CaptureHotkey { get; set; } = Keys.Control | Keys.Shift | Keys.A;

    public ShareXCaptureAction HotkeyAction { get; set; } = ShareXCaptureAction.RegionToFile;

    public SelectionFrameLineStyle SelectionFrameLineStyle { get; set; } = SelectionFrameLineStyle.Solid;

    public string SelectionFrameColor { get; set; } = "#338FF5";

    public double SelectionFrameThickness { get; set; } = 2;

    public bool CaptureCursor { get; set; }

    public bool EnableAnnotations { get; set; } = true;

    public string SaveDirectory { get; set; } = GetDefaultCaptureDirectory();

    public bool OrganizeByMonth { get; set; } = true;

    public bool CloseToTray { get; set; } = true;

    public bool StartWithWindows { get; set; }

    public bool ShowFloatingToolbar { get; set; }

    public string FileNamePattern { get; set; } = "Screenshot_%y-%mo-%d_%h-%mi-%s-%ms";

    private static string GetDefaultCaptureDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ScreenshotAssistant",
            "captures");
}

public enum SelectionFrameLineStyle
{
    Solid,
    Dashed,
    Dotted
}
