# Build script for PDF Signer
# Usage: .\build.ps1 [release|debug|both|clean]

param(
    [Parameter(Position=0)]
    [ValidateSet('release', 'debug', 'both', 'clean')]
    [string]$Configuration = 'release'
)

function Build-Release {
    Write-Host "Building Release version..." -ForegroundColor Cyan
    dotnet publish -c Release
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Release build complete!" -ForegroundColor Green
        Write-Host "Output: bin\Release\net8.0\win-x64\publish\PdfSigner.exe" -ForegroundColor Yellow
        $size = (Get-Item "bin\Release\net8.0\win-x64\publish\PdfSigner.exe").Length / 1MB
        Write-Host ("Size: {0:N2} MB" -f $size) -ForegroundColor Yellow
        Write-Host "========================================" -ForegroundColor Green
    }
}

function Build-Debug {
    Write-Host "Building Debug version..." -ForegroundColor Cyan
    dotnet publish -c Debug
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "Debug build complete!" -ForegroundColor Green
        Write-Host "Output: bin\Debug\net8.0\win-x64\publish\PdfSigner.exe" -ForegroundColor Yellow
        $size = (Get-Item "bin\Debug\net8.0\win-x64\publish\PdfSigner.exe").Length / 1MB
        Write-Host ("Size: {0:N2} MB" -f $size) -ForegroundColor Yellow
        Write-Host "========================================" -ForegroundColor Green
    }
}

function Clean-Build {
    Write-Host "Cleaning build outputs..." -ForegroundColor Cyan
    dotnet clean
    if (Test-Path "bin") { Remove-Item -Path "bin" -Recurse -Force }
    if (Test-Path "obj") { Remove-Item -Path "obj" -Recurse -Force }
    Write-Host "Clean complete." -ForegroundColor Green
}

switch ($Configuration) {
    'release' { Build-Release }
    'debug' { Build-Debug }
    'both' { 
        Build-Release
        Write-Host ""
        Build-Debug
    }
    'clean' { Clean-Build }
}
