# PDF Signer with Windows Certificate Store - AI Coding Guide

## Project Overview
Console application for digitally signing PDFs using Windows Certificate Store certificates. Core tech: .NET 9.0, iText 9.3.0, BouncyCastle for cryptography. Single-file standalone executable (~40MB) with custom icon, comprehensive test suite (XUnit, FluentAssertions), and professional branding.

## Architecture & Critical Patterns

### Three-Component Design
1. **Program.cs**: CLI entry point - handles argument parsing, command routing (`list`, `sign`, `batch`, `verify`). Default location: "PdfSigner by rysiok"
2. **WindowsCertificatePdfSigner.cs**: Core library (740 lines) - all PDF signing/verification logic
3. **Test Suite** (`PdfSigner.Tests/`): 82 tests organized by concern (signing, verification, error scenarios)

### Key Architectural Decisions

**Signature Preservation via Append Mode** (lines 620-625 in WindowsCertificatePdfSigner.cs):
```csharp
var stampingProperties = new StampingProperties();
stampingProperties.UseAppendMode();  // Critical: preserves existing signatures
```
When signing already-signed PDFs, append mode is REQUIRED to maintain previous signatures. This is essential for qualified electronic signature workflows where multiple parties sign the same document.

**Multi-Signature Verification Logic** (lines 520-615):
The `VerifySignature` method iterates through ALL signatures to find the one matching the provided certificate. This is non-obvious: when a PDF has multiple signatures, only the latest covers the whole document. Earlier signatures become "partial" but remain cryptographically valid.

**SERIALNUMBER as Identity** (lines 490-510):
Verification relies on the SERIALNUMBER property in certificate subjects (e.g., `CN=Name, SERIALNUMBER=123456`). The `ExtractSerialNumberFromSubject` method handles multiple DN formats (comma, semicolon, OID.2.5.4.5) because Windows certificate stores can reformat subject strings.

## Development Workflows

### Building Release Executable
```powershell
.\build.ps1 release              # Single-file, self-contained, compressed (~40MB)
.\build.ps1 debug                # Debug version with symbols
.\build.ps1 both                 # Both release and debug
.\build.ps1 clean                # Clean all build outputs
```
Output: `bin\Release\net9.0\win-x64\publish\PdfSigner.exe`

**Build Configuration** (PdfSigner.csproj):
- Release: `PublishSingleFile=true`, `EnableCompressionInSingleFile=true`
- Icon: `icon.ico` (blue document with signature checkmark)
- Version: 1.0.0.0, Product: "PDF Signer with Windows Certificate Store"
- No trimming (iText 9.3.0 uses reflection extensively)

### Running Tests
```powershell
dotnet test --verbosity minimal --nologo                    # Quick test run
dotnet test --collect:"XPlat Code Coverage"                 # With coverage
dotnet test --filter "FullyQualifiedName~DoubleSign"        # Single test
```
Current status: 78 passing, 4 skipped (investigation needed for X509Certificate2.Subject format quirks)

### Test Certificate Generation
Tests use `TestCertificateGenerator.CreateCertificateWithSerialNumber()` to create BouncyCastle self-signed certs with custom SERIALNUMBER properties. These are installed to Windows cert store during test setup and cleaned up via IDisposable pattern.

**Certificate Cleanup**: Test certificates should be automatically removed by the IDisposable pattern, but if test runs are interrupted or fail, certificates may accumulate in the Windows Certificate Store. To clean up orphaned test certificates:
```powershell
# Remove all test certificates (ERR123, BATCH123, PUB123, etc.)
Get-ChildItem Cert:\CurrentUser\My | Where-Object { 
    $_.Subject -match "SERIALNUMBER=(ERR123|BATCH123|PUB123|SIGN123456|SECOND987654|SECOND789|DIFFERENT123)" -or 
    $_.Subject -match "CN=(SigningTestCert|SecondSignerCert|ErrorTestCert|BatchTestCert|PublicOnlyCert|AnotherCert|SecondCert)(?:,|$)" 
} | Remove-Item -Force
```
This command identifies test certificates by their SERIALNUMBER or CN patterns and removes them from the CurrentUser\My certificate store.

### Runtime Commands
```powershell
dotnet run list                                              # List available certificates
dotnet run sign input.pdf output.pdf "cert_subject"         # Sign single file
dotnet run batch "*.pdf" "output_dir" "cert_subject"        # Batch signing

# Or use built executable
.\bin\Release\net8.0\win-x64\publish\PdfSigner.exe list
```

## Project-Specific Conventions

### Default Values
- **Location**: "PdfSigner by rysiok" (not computer name)
- **Reason**: "Document digitally signed"
- **Output suffix**: "-sig" (for batch operations)

### Exception Handling Strategy
- Public methods throw `InvalidOperationException` with descriptive messages for library consumers
- Private methods may throw various exceptions, caught and wrapped by public methods
- Certificate not found, file not found, and verification failures all use `InvalidOperationException`

### Certificate Finding Logic (lines 220-280)
Three search strategies in order:
1. **Thumbprint match**: Hex string 32-128 chars → exact match via `FindByThumbprint`
2. **Subject DN exact match**: `FindBySubjectDistinguishedName`
3. **Partial subject match**: `FindBySubjectName` (fallback for user-friendly input like "John Doe")

Searches CurrentUser store first, then LocalMachine. Returns first valid cert with private key.

### Test Organization Pattern
- **PdfSigningTests.cs**: Single file operations, batch operations, certificate validation, double-signing
- **PdfVerificationTests.cs**: Signature verification scenarios (4 tests currently skipped)
- **ErrorScenarioTests.cs**: Exception handling, invalid inputs
- **Utilities/**: `TestCertificateGenerator` (BouncyCastle cert creation), `TestPdfGenerator` (iText7 test PDFs)

Each test class implements `IDisposable` for cert cleanup and temp directory removal.

### Result Classes (lines 706-735)
- `PdfVerificationResult`: Multiple signature verification (used by `VerifyPdfSignature`)
- `SignatureVerificationResult`: Single signature verification (used by `VerifySignature`)
- `SignatureInfo`: Per-signature details (valid/invalid, subject, serial number, error message)

## Critical Integration Points

### iText7 + BouncyCastle Bridge
The `DotNetSignature` class (lines 640-700) bridges .NET crypto APIs to iText7's BouncyCastle expectations:
- Implements `IExternalSignature` interface
- Handles both RSA and ECDSA private keys
- Uses `RSA.SignData()` or `ECDsa.SignData()` with SHA256

### Windows Certificate Store Access
Uses `X509Store(StoreName.My, StoreLocation)` for certificate discovery. Requires:
- `OpenFlags.ReadOnly` for non-admin access
- Explicit store.Close() in finally blocks (even though disposable, to avoid handle leaks)
- Checks for `HasPrivateKey` and validity dates before returning certificates

## Common Pitfalls

1. **Forgetting UseAppendMode()**: Without it, signing destroys existing signatures
2. **SERIALNUMBER extraction complexity**: X509Certificate2.Subject doesn't preserve DN order reliably - always use `ExtractSerialNumberFromSubject()`
3. **Verification after append**: The first signature won't "cover whole document" after second signature added - this is CORRECT behavior, not a bug
4. **Test certificate lifecycle**: Always install certs to store AND track IDisposable cleanup to avoid cert store pollution between tests
5. **Build configuration**: Always specify `PdfSigner.csproj` explicitly when building in directories with multiple projects

## Test Coverage Strategy
Target: >85% line coverage for WindowsCertificatePdfSigner.cs (currently 86.8%). Focus on:
- Certificate finding edge cases (expired, no private key, multiple matches)
- Signature verification with multiple signers
- Error conditions (missing files, invalid PDFs, corrupt signatures)
- Batch operation scenarios (empty patterns, mixed success/failure)

## Application Identity & Branding
- **Icon**: `icon.ico` - Blue document with signature checkmark (create-icon.ps1 to regenerate)
- **Version Info**: Product name, version 1.0.0, copyright embedded in executable
- **Default Location**: "PdfSigner by rysiok" appears in PDF signature metadata
- **Executable Size**: ~40MB compressed (Release), includes .NET runtime + all dependencies

## Quick Reference Commands
```powershell
# Development cycle
.\build.ps1 release && .\bin\Release\net8.0\win-x64\publish\PdfSigner.exe sign test.pdf signed.pdf "thumbprint"

# Test with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Check specific test output
dotnet test --logger "console;verbosity=detailed" --filter "SignPdf_DoubleSign"

# Recreate application icon
.\create-icon.ps1

# View executable properties
explorer.exe /select,"bin\Release\net8.0\win-x64\publish\PdfSigner.exe"
```
