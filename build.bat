@echo off
REM Build script for PDF Signer
REM Usage: build.bat [release|debug|both|clean]

set CONFIG=%1

if "%CONFIG%"=="" set CONFIG=release

if /i "%CONFIG%"=="clean" goto clean
if /i "%CONFIG%"=="release" goto release
if /i "%CONFIG%"=="debug" goto debug
if /i "%CONFIG%"=="both" goto both

:usage
echo Usage: build.bat [release^|debug^|both^|clean]
echo.
echo   release - Build optimized single-file executable (default)
echo   debug   - Build debug single-file executable with symbols
echo   both    - Build both release and debug versions
echo   clean   - Clean all build outputs
goto end

:clean
echo Cleaning build outputs...
dotnet clean
if exist "bin" rmdir /s /q bin
if exist "obj" rmdir /s /q obj
echo Clean complete.
goto end

:release
echo Building Release version...
dotnet publish PdfSigner.csproj -c Release
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Release build complete!
    echo Output: bin\Release\net8.0\win-x64\publish\PdfSigner.exe
    echo ========================================
)
goto end

:debug
echo Building Debug version...
dotnet publish PdfSigner.csproj -c Debug
if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Debug build complete!
    echo Output: bin\Debug\net8.0\win-x64\publish\PdfSigner.exe
    echo ========================================
)
goto end

:both
echo Building both Release and Debug versions...
echo.
call :release
echo.
call :debug
goto end

:end
