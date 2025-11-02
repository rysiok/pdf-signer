# Script to create a simple PDF signature icon
# This creates a basic icon - you should replace it with a professional icon

Add-Type -AssemblyName System.Drawing

# Create a 256x256 bitmap
$size = 256
$bitmap = New-Object System.Drawing.Bitmap($size, $size)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)

# Enable anti-aliasing
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAlias

# Fill background with gradient
$rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
$brush1 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 41, 128, 185))  # Blue
$brush2 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 52, 152, 219))  # Lighter blue
$graphics.FillRectangle($brush1, $rect)

# Draw document shape (rectangle with folded corner)
$docRect = New-Object System.Drawing.Rectangle(48, 40, 160, 200)
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$graphics.FillRectangle($whiteBrush, $docRect)

# Draw folded corner
$cornerPoints = @(
    [System.Drawing.Point]::new(208, 40),
    [System.Drawing.Point]::new(178, 40),
    [System.Drawing.Point]::new(208, 70)
)
$graphics.FillPolygon($whiteBrush, $cornerPoints)

# Draw corner shadow
$grayBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 200, 200, 200))
$cornerShadow = @(
    [System.Drawing.Point]::new(178, 40),
    [System.Drawing.Point]::new(208, 40),
    [System.Drawing.Point]::new(208, 70),
    [System.Drawing.Point]::new(178, 70)
)
$graphics.FillPolygon($grayBrush, $cornerShadow)

# Draw document border
$borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 100, 100, 100), 2)
$graphics.DrawRectangle($borderPen, $docRect)

# Draw signature icon (pen/checkmark)
$signPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 41, 128, 185), 8)
$signPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$signPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

# Draw stylized signature/checkmark
$graphics.DrawLine($signPen, 70, 150, 100, 180)
$graphics.DrawLine($signPen, 100, 180, 190, 100)

# Draw wavy line (signature line)
$linePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 150, 150, 150), 2)
$graphics.DrawLine($linePen, 60, 200, 200, 200)

# Save as PNG first (higher quality)
$pngPath = "C:\git\sign\icon_temp.png"
$bitmap.Save($pngPath, [System.Drawing.Imaging.ImageFormat]::Png)

Write-Host "Creating icon file..." -ForegroundColor Cyan

# Convert to ICO format with multiple sizes
$iconSizes = @(16, 32, 48, 64, 128, 256)
$iconPath = "C:\git\sign\icon.ico"

# Use .NET to create proper ICO file
$icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())

# Save the icon
$fileStream = New-Object System.IO.FileStream($iconPath, [System.IO.FileMode]::Create)
$icon.Save($fileStream)
$fileStream.Close()

# Cleanup
$graphics.Dispose()
$bitmap.Dispose()
$icon.Dispose()
Remove-Item $pngPath -ErrorAction SilentlyContinue

if (Test-Path $iconPath) {
    Write-Host "✓ Icon created successfully: $iconPath" -ForegroundColor Green
    Write-Host ""
    Write-Host "NOTE: This is a basic icon. For production use, consider:" -ForegroundColor Yellow
    Write-Host "  - Using a professional icon designer" -ForegroundColor Yellow
    Write-Host "  - Online icon generators (e.g., icoconvert.com)" -ForegroundColor Yellow
    Write-Host "  - Icon design software (e.g., GIMP, Inkscape)" -ForegroundColor Yellow
} else {
    Write-Host "✗ Failed to create icon file" -ForegroundColor Red
}
