$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$outputPath = Join-Path $repoRoot "src\OpenPanel.Host\Assets\OpenPanel.ico"
$outputDirectory = Split-Path $outputPath -Parent
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$bitmap = [System.Drawing.Bitmap]::new(
    256,
    256,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.Clear([System.Drawing.Color]::Transparent)

$panelPath = [System.Drawing.Drawing2D.GraphicsPath]::new()
$panelPath.AddArc(18, 28, 34, 34, 180, 90)
$panelPath.AddArc(204, 28, 34, 34, 270, 90)
$panelPath.AddArc(204, 154, 34, 34, 0, 90)
$panelPath.AddArc(18, 154, 34, 34, 90, 90)
$panelPath.CloseFigure()

$panelBrush = [System.Drawing.SolidBrush]::new(
    [System.Drawing.Color]::FromArgb(255, 7, 11, 16))
$accentPen = [System.Drawing.Pen]::new(
    [System.Drawing.Color]::FromArgb(255, 46, 211, 198),
    13)
$accentPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$accentPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$accentPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$pulsePen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, 12)
$pulsePen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$pulsePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$pulsePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

$graphics.FillPath($panelBrush, $panelPath)
$graphics.DrawPath($accentPen, $panelPath)
$graphics.DrawLines(
    $pulsePen,
    [System.Drawing.PointF[]] @(
        [System.Drawing.PointF]::new(42, 111),
        [System.Drawing.PointF]::new(76, 111),
        [System.Drawing.PointF]::new(96, 78),
        [System.Drawing.PointF]::new(122, 145),
        [System.Drawing.PointF]::new(148, 96),
        [System.Drawing.PointF]::new(178, 96),
        [System.Drawing.PointF]::new(196, 72),
        [System.Drawing.PointF]::new(216, 72)
    ))
$graphics.DrawLine($accentPen, 128, 188, 128, 220)
$graphics.DrawLine($accentPen, 88, 220, 168, 220)

$pngStream = [System.IO.MemoryStream]::new()
$bitmap.Save($pngStream, [System.Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $pngStream.ToArray()

$fileStream = [System.IO.File]::Create($outputPath)
$writer = [System.IO.BinaryWriter]::new($fileStream)
try {
    $writer.Write([uint16] 0)
    $writer.Write([uint16] 1)
    $writer.Write([uint16] 1)
    $writer.Write([byte] 0)
    $writer.Write([byte] 0)
    $writer.Write([byte] 0)
    $writer.Write([byte] 0)
    $writer.Write([uint16] 1)
    $writer.Write([uint16] 32)
    $writer.Write([uint32] $pngBytes.Length)
    $writer.Write([uint32] 22)
    $writer.Write($pngBytes)
}
finally {
    $writer.Dispose()
    $fileStream.Dispose()
    $pngStream.Dispose()
    $pulsePen.Dispose()
    $accentPen.Dispose()
    $panelBrush.Dispose()
    $panelPath.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

Write-Host "Generated $outputPath"
