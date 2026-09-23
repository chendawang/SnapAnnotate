using ScreenshotAssistant.Capture;

namespace ScreenshotAssistant.Capture.Tests;

public sealed class ScreenshotAssistantSettingsTests
{
    [Fact]
    public void NewSettings_KeepsSelectionOpenForToolbar()
    {
        ScreenshotAssistantSettings settings = new();

        Assert.False(settings.RegionCapture.QuickCapture);
        Assert.Equal(ShareXCaptureAction.RegionToFile, settings.HotkeyAction);
        Assert.Equal(SelectionFrameLineStyle.Solid, settings.SelectionFrameLineStyle);
        Assert.Equal("#338FF5", settings.SelectionFrameColor);
        Assert.Equal(2, settings.SelectionFrameThickness);
    }

    [Fact]
    public void SaveAndLoad_PreservesShareXOptions()
    {
        using TemporaryDirectory directory = new();
        string settingsPath = Path.Combine(directory.Path, "Settings.json");
        ScreenshotAssistantSettings settings = new()
        {
            SaveDirectory = @"D:\Captures",
            FileNamePattern = "capture_%y-%mo-%d",
            EnableAnnotations = false,
            CloseToTray = false,
            StartWithWindows = true
        };
        settings.RegionCapture.DetectControls = false;
        settings.RegionCapture.ShowInfo = false;
        settings.HotkeyAction = ShareXCaptureAction.RegionToFile;
        settings.ShowFloatingToolbar = true;
        settings.ImageEditor.Thickness = 9;
        settings.SelectionFrameLineStyle = SelectionFrameLineStyle.Dashed;
        settings.SelectionFrameColor = "#FF5500";
        settings.SelectionFrameThickness = 4;

        Assert.True(settings.Save(settingsPath));

        ScreenshotAssistantSettings loaded = ScreenshotAssistantSettings.Load(settingsPath);
        Assert.Equal(@"D:\Captures", loaded.SaveDirectory);
        Assert.Equal("capture_%y-%mo-%d", loaded.FileNamePattern);
        Assert.False(loaded.EnableAnnotations);
        Assert.False(loaded.CloseToTray);
        Assert.True(loaded.StartWithWindows);
        Assert.False(loaded.RegionCapture.DetectControls);
        Assert.False(loaded.RegionCapture.ShowInfo);
        Assert.Equal(ShareXCaptureAction.RegionToFile, loaded.HotkeyAction);
        Assert.True(loaded.ShowFloatingToolbar);
        Assert.Equal(9, loaded.ImageEditor.Thickness);
        Assert.Equal(SelectionFrameLineStyle.Dashed, loaded.SelectionFrameLineStyle);
        Assert.Equal("#FF5500", loaded.SelectionFrameColor);
        Assert.Equal(4, loaded.SelectionFrameThickness);
    }

    [Fact]
    public void Load_WhenPrimaryIsCorrupt_UsesShareXBackupFallback()
    {
        using TemporaryDirectory directory = new();
        string settingsPath = Path.Combine(directory.Path, "Settings.json");
        string backupDirectory = Path.Combine(directory.Path, "backups");
        ScreenshotAssistantSettings settings = new()
        {
            BackupFolder = backupDirectory,
            CreateBackup = true,
            SaveDirectory = "first"
        };

        Assert.True(settings.Save(settingsPath));
        settings.SaveDirectory = "second";
        Assert.True(settings.Save(settingsPath));
        File.WriteAllText(settingsPath, "{ invalid json");

        ScreenshotAssistantSettings loaded = ScreenshotAssistantSettings.Load(
            settingsPath,
            backupDirectory);
        Assert.Equal("first", loaded.SaveDirectory);
    }

    [Fact]
    public void TryCreateFileNamePreview_ParsesShareXVariables()
    {
        bool success = ShareXSettingsService.TryCreateFileNamePreview(
            "capture_%widthx%height_%y-%mo-%d",
            out string preview,
            out string error);

        Assert.True(success, error);
        Assert.Contains("1920x1080", preview, StringComparison.Ordinal);
        Assert.EndsWith(".png", preview, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid:name")]
    public void TryCreateFileNamePreview_RejectsInvalidPatterns(string pattern)
    {
        bool success = ShareXSettingsService.TryCreateFileNamePreview(
            pattern,
            out string preview,
            out string error);

        Assert.False(success);
        Assert.Empty(preview);
        Assert.NotEmpty(error);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "ScreenshotAssistant.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
