using System.Drawing;
using Avalonia.Controls;
using ScreenshotAssistant.Core.Capture;
using ShareX.HelpersLib;
using ShareX.ImageEditor.Integration;
using ShareX.ScreenCaptureLib;
using ShareX.ScreenCaptureLib.Presentation.RegionCapture;
using SkiaSharp;

namespace ScreenshotAssistant.Capture;

public sealed class ShareXRegionCaptureWorkflow(
    ShareXHistoryService historyService,
    ShareXSettingsService settingsService)
{
    public async Task<CaptureResult> CaptureAsync(
        Window owner,
        ShareXCaptureAction action = ShareXCaptureAction.RegionToFile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(owner);
        cancellationToken.ThrowIfCancellationRequested();

        using CaptureOwnerVisibility ownerVisibility = new(owner);

        await ownerVisibility.HideAsync(cancellationToken);
            ShareXCaptureAction completedAction = action;
            SKBitmap? capturedBitmap;
            if (action == ShareXCaptureAction.FullscreenToFile)
            {
                capturedBitmap = CaptureFullscreen(settingsService.Current);
            }
            else
            {
                ShareXRegionCaptureSessionResult regionResult =
                    await CaptureRegionWithShareXAsync(settingsService.Current, action);
                capturedBitmap = regionResult.Capture?.Image;
                completedAction = regionResult.Action;
            }

            using SKBitmap? capturedImage = capturedBitmap;
            if (capturedImage is null)
            {
                return CaptureResult.Cancelled(DateTimeOffset.UtcNow);
            }

            cancellationToken.ThrowIfCancellationRequested();
            DateTimeOffset completedAt = DateTimeOffset.UtcNow;
            using Bitmap image = GdiSkiaBitmapConverter.ToGdiBitmap(capturedImage);

            if (completedAction == ShareXCaptureAction.RegionToClipboard)
            {
                if (!ClipboardHelpers.CopyImage(image, "Screenshot.png"))
                {
                    throw new InvalidOperationException("ShareX 无法把截图写入剪贴板。");
                }

                settingsService.Save();
                return CaptureResult.CopiedToClipboard(completedAt);
            }

            string outputPath = SavePng(image, completedAt);
            historyService.Record(outputPath, completedAt, image.Width, image.Height);
            settingsService.Save();
            if (!ClipboardHelpers.CopyImage(image, "Screenshot.png"))
            {
                throw new InvalidOperationException(
                    $"截图已保存到 {outputPath}，但 ShareX 无法把截图写入剪贴板。");
            }

            return CaptureResult.Completed(completedAt, outputPath);
    }

    private static Task<ShareXRegionCaptureSessionResult> CaptureRegionWithShareXAsync(
        ScreenshotAssistantSettings settings,
        ShareXCaptureAction action)
    {
        // PixPin-style interaction requires the selection to remain active so the
        // annotation/output toolbar can be used after releasing the mouse.
        settings.RegionCapture.QuickCapture = false;

        Screenshot screenshot = new()
        {
            CaptureCursor = settings.CaptureCursor
        };
        Rectangle screenBounds = settings.RegionCapture.ActiveMonitorMode
            ? CaptureHelpers.GetActiveScreenBounds()
            : CaptureHelpers.GetScreenBounds();
        SKBitmap frozenScreenshot;

        using (Bitmap canvas = settings.RegionCapture.ActiveMonitorMode
            ? screenshot.CaptureActiveMonitor()
            : screenshot.CaptureFullscreen())
        {
            frozenScreenshot = GdiSkiaBitmapConverter.ToSKBitmap(canvas);
        }

        AvaloniaRegionCaptureRequest request = new()
        {
            Screenshot = frozenScreenshot,
            ScreenBounds = screenBounds,
            RegionCaptureOptions = settings.RegionCapture,
            ImageEditorOptions = settings.ImageEditor,
            EnableAnnotations = settings.EnableAnnotations
        };

        SelectionFrameAppearance selectionFrame = new(
            settings.SelectionFrameColor,
            settings.SelectionFrameThickness,
            settings.SelectionFrameLineStyle);
        return ShareXRegionCaptureSession.CaptureAsync(request, action, selectionFrame);
    }

    private static SKBitmap CaptureFullscreen(ScreenshotAssistantSettings settings)
    {
        Screenshot screenshot = new()
        {
            CaptureCursor = settings.CaptureCursor
        };

        using Bitmap canvas = screenshot.CaptureFullscreen();
        return GdiSkiaBitmapConverter.ToSKBitmap(canvas);
    }

    private string SavePng(Bitmap image, DateTimeOffset capturedAt)
    {
        ScreenshotAssistantSettings settings = settingsService.Current;
        string outputDirectory = settings.OrganizeByMonth
            ? Path.Combine(
                settings.SaveDirectory,
                capturedAt.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture),
                capturedAt.ToString("MM", System.Globalization.CultureInfo.InvariantCulture))
            : settings.SaveDirectory;
        Directory.CreateDirectory(outputDirectory);

        NameParser nameParser = new(NameParserType.FileName)
        {
            ImageWidth = image.Width,
            ImageHeight = image.Height
        };
        string fileName = nameParser.Parse(settings.FileNamePattern) + ".png";
        string outputPath = FileHelpers.GetUniqueFilePath(Path.Combine(outputDirectory, fileName));

        if (!ImageHelpers.SaveImage(image, outputPath))
        {
            throw new IOException($"ShareX 无法保存截图：{outputPath}");
        }

        return outputPath;
    }
}
