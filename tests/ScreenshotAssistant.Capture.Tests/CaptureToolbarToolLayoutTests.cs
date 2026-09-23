using ShareX.ImageEditor.Core.Annotations;

namespace ScreenshotAssistant.Capture.Tests;

public sealed class CaptureToolbarToolLayoutTests
{
    [Fact]
    public void Primary_ContainsOnlyFrequentAnnotationTools()
    {
        Assert.Equal(
            [EditorTool.Rectangle, EditorTool.Arrow, EditorTool.Freehand, EditorTool.Text],
            CaptureToolbarToolLayout.Primary);
    }

    [Fact]
    public void More_ContainsInfrequentToolsIncludingSpotlight()
    {
        EditorTool[] movedTools =
        [
            EditorTool.SpeechBalloon,
            EditorTool.Step,
            EditorTool.Image,
            EditorTool.Emoji,
            EditorTool.Cursor,
            EditorTool.Highlight,
            EditorTool.SmartEraser,
            EditorTool.Blur,
            EditorTool.Pixelate,
            EditorTool.Magnify,
            EditorTool.Spotlight
        ];

        Assert.All(movedTools, tool => Assert.Contains(tool, CaptureToolbarToolLayout.More));
        Assert.All(movedTools, tool => Assert.DoesNotContain(tool, CaptureToolbarToolLayout.Primary));
    }
}
