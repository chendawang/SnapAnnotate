param(
    [string] $OutputPath = (Join-Path $PSScriptRoot '..\src\ScreenshotAssistant.App\Assets\ScreenshotAssistant.ico')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath {
    param(
        [System.Drawing.RectangleF] $Rectangle,
        [float] $Radius
    )

    $path = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = $Radius * 2
    $arc = [System.Drawing.RectangleF]::new($Rectangle.X, $Rectangle.Y, $diameter, $diameter)
    $path.AddArc($arc, 180, 90)
    $arc.X = $Rectangle.Right - $diameter
    $path.AddArc($arc, 270, 90)
    $arc.Y = $Rectangle.Bottom - $diameter
    $path.AddArc($arc, 0, 90)
    $arc.X = $Rectangle.X
    $path.AddArc($arc, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconPngBytes {
    param([int] $Size)

    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $scale = $Size / 512.0

        $backgroundPath = New-RoundedRectanglePath ([System.Drawing.RectangleF]::new(24 * $scale, 24 * $scale, 464 * $scale, 464 * $scale)) (112 * $scale)
        $backgroundBrush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
            [System.Drawing.PointF]::new(72 * $scale, 48 * $scale),
            [System.Drawing.PointF]::new(432 * $scale, 464 * $scale),
            [System.Drawing.ColorTranslator]::FromHtml('#55A7FF'),
            [System.Drawing.ColorTranslator]::FromHtml('#286DE8'))
        try {
            $graphics.FillPath($backgroundBrush, $backgroundPath)
        }
        finally {
            $backgroundBrush.Dispose()
            $backgroundPath.Dispose()
        }

        $framePen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, [Math]::Max(1.5, 34 * $scale))
        $framePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $framePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $framePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        try {
            $segments = @(
                @(126, 218, 126, 148, 126, 148, 166, 108, 236, 108),
                @(276, 108, 346, 108, 346, 108, 386, 148, 386, 218),
                @(126, 294, 126, 364, 126, 364, 166, 404, 236, 404),
                @(386, 294, 386, 364, 386, 364, 346, 404, 276, 404)
            )
            foreach ($segment in $segments) {
                for ($index = 0; $index -lt 8; $index += 2) {
                    $graphics.DrawLine(
                        $framePen,
                        $segment[$index] * $scale,
                        $segment[$index + 1] * $scale,
                        $segment[$index + 2] * $scale,
                        $segment[$index + 3] * $scale)
                }
            }
        }
        finally {
            $framePen.Dispose()
        }

        $circleBounds = [System.Drawing.RectangleF]::new(295 * $scale, 286 * $scale, 152 * $scale, 152 * $scale)
        $greenBrush = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#22B573'))
        $whitePen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, [Math]::Max(1.5, 18 * $scale))
        try {
            $graphics.FillEllipse($greenBrush, $circleBounds)
            $graphics.DrawEllipse($whitePen, $circleBounds)
        }
        finally {
            $greenBrush.Dispose()
            $whitePen.Dispose()
        }

        $checkPen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, [Math]::Max(1.5, 22 * $scale))
        $checkPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $checkPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $checkPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        try {
            $checkPoints = [System.Drawing.PointF[]] @(
                [System.Drawing.PointF]::new(334 * $scale, 362 * $scale),
                [System.Drawing.PointF]::new(358 * $scale, 387 * $scale),
                [System.Drawing.PointF]::new(409 * $scale, 331 * $scale))
            $graphics.DrawLines($checkPen, $checkPoints)
        }
        finally {
            $checkPen.Dispose()
        }

        $stream = [IO.MemoryStream]::new()
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        return $stream.ToArray()
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = [System.Collections.Generic.List[byte[]]]::new()
foreach ($size in $sizes) {
    $images.Add((New-IconPngBytes $size))
}
$outputFullPath = [IO.Path]::GetFullPath($OutputPath)
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($outputFullPath)) | Out-Null
$previewPath = [IO.Path]::ChangeExtension($outputFullPath, '.png')
[IO.File]::WriteAllBytes($previewPath, $images[$images.Count - 1])
$file = [IO.File]::Create($outputFullPath)
$writer = [IO.BinaryWriter]::new($file)
try {
    $writer.Write([uint16] 0)
    $writer.Write([uint16] 1)
    $writer.Write([uint16] $images.Count)
    $offset = 6 + (16 * $images.Count)
    for ($index = 0; $index -lt $images.Count; $index++) {
        $size = $sizes[$index]
        $image = $images[$index]
        $writer.Write([byte] $(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([byte] $(if ($size -eq 256) { 0 } else { $size }))
        $writer.Write([byte] 0)
        $writer.Write([byte] 0)
        $writer.Write([uint16] 1)
        $writer.Write([uint16] 32)
        $writer.Write([uint32] $image.Length)
        $writer.Write([uint32] $offset)
        $offset += $image.Length
    }

    foreach ($image in $images) {
        $writer.Write($image)
    }
}
finally {
    $writer.Dispose()
    $file.Dispose()
}

Write-Host "应用图标已生成：$outputFullPath"
