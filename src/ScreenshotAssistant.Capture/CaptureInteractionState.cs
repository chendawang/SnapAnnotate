using Avalonia;

namespace ScreenshotAssistant.Capture;

public sealed class CaptureInteractionState
{
    public bool IsSelectionConfirmed { get; private set; }

    public bool TryConfirm(Rect selection)
    {
        if (IsSelectionConfirmed || selection.Width <= 0 || selection.Height <= 0)
        {
            return false;
        }

        IsSelectionConfirmed = true;
        return true;
    }
}
