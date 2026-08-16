param(
    [string]$SourcePng = "..\Assets\Icons\logo.png",
    [string]$OutputIco = "..\Assets\Icons\app.ico",
    [switch]$EnableCrop,
    [double]$PaddingRatio = 0.06,
    [int]$BlackThreshold = 18
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$sourcePath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot $SourcePng))
$outputPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot $OutputIco))
$outputDir = [System.IO.Path]::GetDirectoryName($outputPath)

if (-not (Test-Path $sourcePath)) {
    throw "Source PNG tidak ditemukan: $sourcePath"
}

if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir | Out-Null
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)

$sourceImage = [System.Drawing.Bitmap]::FromFile($sourcePath)

function Get-ContentBounds {
    param(
        [System.Drawing.Bitmap]$Bitmap,
        [int]$Threshold
    )

    $minX = $Bitmap.Width
    $minY = $Bitmap.Height
    $maxX = -1
    $maxY = -1

    for ($y = 0; $y -lt $Bitmap.Height; $y++) {
        for ($x = 0; $x -lt $Bitmap.Width; $x++) {
            $pixel = $Bitmap.GetPixel($x, $y)
            if ($pixel.A -gt 0 -and ($pixel.R -gt $Threshold -or $pixel.G -gt $Threshold -or $pixel.B -gt $Threshold)) {
                if ($x -lt $minX) { $minX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }

    if ($maxX -lt 0 -or $maxY -lt 0) {
        return [System.Drawing.Rectangle]::new(0, 0, $Bitmap.Width, $Bitmap.Height)
    }

    $contentWidth = $maxX - $minX + 1
    $contentHeight = $maxY - $minY + 1
    $side = [Math]::Max($contentWidth, $contentHeight)
    $padding = [int][Math]::Ceiling($side * $PaddingRatio)
    $squareSide = $side + ($padding * 2)

    $centerX = ($minX + $maxX) / 2.0
    $centerY = ($minY + $maxY) / 2.0

    $left = [int][Math]::Floor($centerX - ($squareSide / 2.0))
    $top = [int][Math]::Floor($centerY - ($squareSide / 2.0))

    if ($left -lt 0) { $left = 0 }
    if ($top -lt 0) { $top = 0 }
    if ($left + $squareSide -gt $Bitmap.Width) { $left = [Math]::Max(0, $Bitmap.Width - $squareSide) }
    if ($top + $squareSide -gt $Bitmap.Height) { $top = [Math]::Max(0, $Bitmap.Height - $squareSide) }

    $width = [Math]::Min($squareSide, $Bitmap.Width - $left)
    $height = [Math]::Min($squareSide, $Bitmap.Height - $top)

    return [System.Drawing.Rectangle]::new($left, $top, $width, $height)
}

function New-ResizedBitmap {
    param(
        [System.Drawing.Image]$Image,
        [System.Drawing.Rectangle]$SourceRect,
        [int]$Size
    )

    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

    $destinationRect = [System.Drawing.Rectangle]::new(0, 0, $Size, $Size)
    $graphics.DrawImage($Image, $destinationRect, $SourceRect, [System.Drawing.GraphicsUnit]::Pixel)
    $graphics.Dispose()

    return $bitmap
}

$pngPayloads = New-Object System.Collections.Generic.List[byte[]]
$tempBitmaps = New-Object System.Collections.Generic.List[System.Drawing.Bitmap]
$sourceRect = if ($EnableCrop) {
    Get-ContentBounds -Bitmap $sourceImage -Threshold $BlackThreshold
}
else {
    [System.Drawing.Rectangle]::new(0, 0, $sourceImage.Width, $sourceImage.Height)
}

try {
    foreach ($size in $sizes) {
        $bitmap = New-ResizedBitmap -Image $sourceImage -SourceRect $sourceRect -Size $size
        $tempBitmaps.Add($bitmap)

        $memoryStream = New-Object System.IO.MemoryStream
        $bitmap.Save($memoryStream, [System.Drawing.Imaging.ImageFormat]::Png)
        $pngPayloads.Add($memoryStream.ToArray())
        $memoryStream.Dispose()
    }

    $fileStream = [System.IO.File]::Open($outputPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    $writer = New-Object System.IO.BinaryWriter $fileStream

    try {
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]$sizes.Count)

        $offset = 6 + (16 * $sizes.Count)
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $size = $sizes[$i]
            $payload = $pngPayloads[$i]

            $writer.Write([Byte]($(if ($size -ge 256) { 0 } else { $size })))
            $writer.Write([Byte]($(if ($size -ge 256) { 0 } else { $size })))
            $writer.Write([Byte]0)
            $writer.Write([Byte]0)
            $writer.Write([UInt16]1)
            $writer.Write([UInt16]32)
            $writer.Write([UInt32]$payload.Length)
            $writer.Write([UInt32]$offset)

            $offset += $payload.Length
        }

        for ($i = 0; $i -lt $pngPayloads.Count; $i++) {
            $writer.Write($pngPayloads[$i])
        }
    }
    finally {
        $writer.Dispose()
        $fileStream.Dispose()
    }
}
finally {
    foreach ($bitmap in $tempBitmaps) {
        $bitmap.Dispose()
    }

    $sourceImage.Dispose()
}

Write-Host "ICO berhasil dibuat:"
Write-Host $outputPath
Write-Host "Ukuran icon:"
Write-Host ($sizes -join ", ")
