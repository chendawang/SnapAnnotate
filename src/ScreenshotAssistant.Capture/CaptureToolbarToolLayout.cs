using ShareX.ImageEditor.Core.Annotations;

namespace ScreenshotAssistant.Capture;

internal static class CaptureToolbarToolLayout
{
    public static IReadOnlyList<EditorTool> Primary { get; } =
    [
        EditorTool.Rectangle,
        EditorTool.Arrow,
        EditorTool.Freehand,
        EditorTool.Text
    ];

    public static IReadOnlyList<EditorTool> More { get; } =
    [
        EditorTool.Ellipse,
        EditorTool.Line,
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
}
