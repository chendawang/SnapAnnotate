using Avalonia;

namespace ScreenshotAssistant.Capture;

public static class CaptureToolbarPlacement
{
    public static Rect ToLogical(Rect physicalSelection, double renderScaling)
    {
        double scale = double.IsFinite(renderScaling) && renderScaling > 0
            ? renderScaling
            : 1;
        return new Rect(
            physicalSelection.X / scale,
            physicalSelection.Y / scale,
            physicalSelection.Width / scale,
            physicalSelection.Height / scale);
    }

    public static Point Calculate(
        Rect selection,
        Size toolbarSize,
        Size surfaceSize,
        double gap = 8,
        double margin = 8)
    {
        double maxX = Math.Max(margin, surfaceSize.Width - toolbarSize.Width - margin);
        double x = Math.Clamp(
            selection.Center.X - toolbarSize.Width / 2,
            margin,
            maxX);

        double below = selection.Bottom + gap;
        double above = selection.Top - toolbarSize.Height - gap;
        double maxY = Math.Max(margin, surfaceSize.Height - toolbarSize.Height - margin);
        double y = below + toolbarSize.Height <= surfaceSize.Height - margin
            ? below
            : Math.Max(margin, above);

        return new Point(x, Math.Min(y, maxY));
    }
}
