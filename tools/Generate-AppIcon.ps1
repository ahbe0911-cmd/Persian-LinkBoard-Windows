Add-Type -AssemblyName System.Drawing
$assetDir = Join-Path $PSScriptRoot "..\src\PersianLinkBoard\Assets"
New-Item -ItemType Directory -Force -Path $assetDir | Out-Null
$outFile = Join-Path $assetDir "App.ico"

$bmp = New-Object System.Drawing.Bitmap 256,256
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::FromArgb(16,26,43))

$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(104,119,245))
$g.FillEllipse($brush, 24,24,208,208)

$inner = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(245,247,251))
$g.FillEllipse($inner, 73,73,110,110)

$linePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(104,119,245),18)
$linePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$linePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawLine($linePen,105,151,162,94)
$g.DrawLine($linePen,132,94,162,94)
$g.DrawLine($linePen,162,94,162,124)

$icon = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
$stream = [System.IO.File]::Create($outFile)
$icon.Save($stream)
$stream.Close()
$g.Dispose()
$bmp.Dispose()
Write-Host "Generated $outFile"
