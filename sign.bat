@echo off
setlocal enabledelayedexpansion

echo PDF Signer - Windows Certificate Store
echo =====================================
echo.

REM Check if dotnet is available
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Error: .NET SDK is not installed or not in PATH
    echo Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download
    exit /b 1
)

REM Check if project is built
if not exist "bin\Debug\net8.0\PdfSigner.dll" (
    echo Building project...
    dotnet build
    if %errorlevel% neq 0 (
        echo Error: Failed to build project
        exit /b 1
    )
    echo.
)

if "%~1"=="" (
    echo Usage: sign.bat [command] [parameters]
    echo.
    echo Commands:
    echo   list                                         - List all available certificates
    echo   sign input.pdf output.pdf "cert_identifier" - Sign a PDF file
    echo   sign input.pdf output.pdf "cert_identifier" "reason" "location" - Sign with custom reason and location
    echo.
    echo Examples:
    echo   sign.bat list
    echo   sign.bat sign document.pdf signed_doc.pdf "localhost"
    echo   sign.bat sign contract.pdf signed_contract.pdf "John Doe" "Contract signature" "New York"
    echo   sign.bat sign document.pdf signed_doc.pdf "A6B149D4A2C7D5F3C5E777640B6534652A674040"
    echo.
    echo Certificate identifier options:
    echo   - Subject names: "localhost", "John Doe", "CN=John Doe, O=Company"
    echo   - Thumbprints: "A6B149D4A2C7D5F3C5E777640B6534652A674040"
    echo   - Partial names: "John" to find "CN=John Doe"
    echo.
    goto :eof
)

REM Validate sign command parameters
if "%~1"=="sign" (
    if "%~2"=="" (
        echo Error: Input PDF file not specified
        echo Usage: sign.bat sign input.pdf output.pdf "cert_identifier"
        exit /b 1
    )
    if "%~3"=="" (
        echo Error: Output PDF file not specified
        echo Usage: sign.bat sign input.pdf output.pdf "cert_identifier"
        exit /b 1
    )
    if "%~4"=="" (
        echo Error: Certificate identifier not specified
        echo Usage: sign.bat sign input.pdf output.pdf "cert_identifier"
        exit /b 1
    )
    if not exist "%~2" (
        echo Error: Input file "%~2" not found
        exit /b 1
    )
)

echo Running: dotnet run %*
echo.
dotnet run %*

if %errorlevel% neq 0 (
    echo.
    echo Operation failed with error code %errorlevel%
) else (
    echo.
    echo Operation completed successfully
) 