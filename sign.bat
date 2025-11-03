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
if not exist "bin\Debug\net9.0\PdfSigner.dll" (
    echo Building project...
    dotnet build PdfSigner.csproj
    if %errorlevel% neq 0 (
        echo Error: Failed to build project
        exit /b 1
    )
    echo.
)

if "%~1"=="" (
    echo Usage: sign.bat [command] [parameters] [--output file] [--console]
    echo.
    echo Commands:
    echo   list                                         - List all available certificates
    echo   sign input.pdf output.pdf "cert_identifier" - Sign a PDF file
    echo   sign input.pdf output.pdf "cert_identifier" "reason" "location" - Sign with custom reason and location
    echo   batch "pattern" "output_dir" "cert_identifier" - Sign multiple PDF files
    echo   verify signed.pdf                           - Verify signatures in a PDF file
    echo.
    echo Global Options:
    echo   --output file, -o file                       - Write output to file instead of console
    echo   --console, -c                                - Also write to console when using --output
    echo.
    echo Examples:
    echo   sign.bat list
    echo   sign.bat list --output certificates.txt
    echo   sign.bat list -o certificates.txt -c
    echo   sign.bat sign document.pdf signed_doc.pdf "localhost"
    echo   sign.bat sign contract.pdf signed_contract.pdf "John Doe" "Contract signature" "New York"
    echo   sign.bat sign document.pdf signed_doc.pdf "A6B149D4A2C7D5F3C5E777640B6534652A674040"
    echo   sign.bat sign document.pdf signed_doc.pdf "localhost" -o sign_log.txt
    echo   sign.bat sign document.pdf signed_doc.pdf "localhost" -o sign_log.txt --console
    echo   sign.bat batch "*.pdf" "signed" "localhost"
    echo   sign.bat batch "documents\*.pdf" "output" "John Doe" "Batch signed" "Office" "-approved"
    echo   sign.bat batch "*.pdf" "signed" "localhost" --output batch_log.txt
    echo   sign.bat batch "*.pdf" "signed" "localhost" -o results.txt -c
    echo   sign.bat verify signed_document.pdf
    echo   sign.bat verify signed_document.pdf -o verification.txt
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

REM Validate batch command parameters
if "%~1"=="batch" (
    if "%~2"=="" (
        echo Error: Input pattern not specified
        echo Usage: sign.bat batch "input_pattern" "output_directory" "cert_identifier"
        exit /b 1
    )
    if "%~3"=="" (
        echo Error: Output directory not specified
        echo Usage: sign.bat batch "input_pattern" "output_directory" "cert_identifier"
        exit /b 1
    )
    if "%~4"=="" (
        echo Error: Certificate identifier not specified
        echo Usage: sign.bat batch "input_pattern" "output_directory" "cert_identifier"
        exit /b 1
    )
)

echo Running: dotnet run --project PdfSigner.csproj %*
echo.
dotnet run --project PdfSigner.csproj %*

if %errorlevel% neq 0 (
    echo.
    echo Operation failed with error code %errorlevel%
) else (
    echo.
    echo Operation completed successfully
) 