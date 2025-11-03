# PDF Signer with Windows Certificate Store

This C# console application allows you to digitally sign PDF files using certificates from the Windows Certificate Store. It uses the iText library for PDF manipulation and BouncyCastle for cryptographic operations.

## Features

- **Certificate Discovery**: Automatically finds certificates from Windows Certificate Store (both Current User and Local Machine stores)
- **PDF Signing**: Signs PDF files with digital signatures using X.509 certificates
- **Signature Verification**: Automatically verifies signatures after signing and validates serial numbers
- **Batch Signing**: Sign multiple PDF files at once using pattern matching
- **Certificate Listing**: Lists all available certificates with their details
- **Command Line Interface**: Easy-to-use CLI for batch operations
- **Output Redirection**: Optional file output for automation and logging
- **Batch Processing**: Sign multiple PDF files with a single command
- **Standalone Verification**: Verify signatures in existing signed PDF files
- **Exception-Based Error Handling**: Uses exceptions for better library integration and error reporting
- **Structured Results**: Returns detailed verification results with certificate information

## Prerequisites

- .NET 9.0 or later
- Windows operating system (for certificate store access)
- Valid X.509 certificate with private key in Windows Certificate Store
- **Certificate must have SERIALNUMBER property in subject DN** (required for verification)

## Installation

1. Clone or download the project
2. Navigate to the project directory
3. Restore dependencies and build:
   ```bash
   dotnet restore
   dotnet build
   ```

## Usage

### Global Options

All commands support an optional output file parameter:

```bash
--output <file>    # Write output to file instead of console
-o <file>          # Short form
```

**Examples:**
```bash
# Save certificate list to file
dotnet run list --output certificates.txt

# Save signing log
dotnet run sign document.pdf signed.pdf "John Doe" -o sign_log.txt

# Save batch operation results
dotnet run batch "*.pdf" "output" "localhost" --output batch_results.txt

# Save verification results
dotnet run verify signed.pdf -o verification.txt
```

### List Available Certificates

To see all certificates available in your Windows Certificate Store:

```bash
dotnet run list
```

This will display certificates from both Current User and Local Machine personal stores, showing:
- Subject name
- Issuer
- Serial number
- Validity period
- Whether it has a private key
- Thumbprint

### Sign a PDF File

To sign a PDF file:

```bash
dotnet run sign <input.pdf> <output.pdf> <certificate_subject> [reason] [location] [--output <file>]
```

### Batch Sign Multiple PDF Files

To sign multiple PDF files at once:

```bash
dotnet run batch <input_pattern> <output_directory> <certificate_subject> [reason] [location] [suffix] [--output <file>]
```

**Parameters:**
- `<input.pdf>`: Path to the PDF file you want to sign
- `<output.pdf>`: Path where the signed PDF will be saved
- `<certificate_identifier>`: Certificate identifier - can be:
  - **Subject name**: Full distinguished name (e.g., "CN=John Doe, O=Company")
  - **Partial subject name**: Part of the subject (e.g., "John Doe" or "localhost")
  - **Thumbprint**: Certificate thumbprint (e.g., "A6B149D4A2C7D5F3C5E777640B6534652A674040")
- `[reason]` (optional): Reason for signing (default: "Document digitally signed")
- `[location]` (optional): Location of signing (default: "PdfSigner by rysiok")
- `[suffix]` (optional, batch only): Suffix for output filenames (default: "-sig")

**Examples:**

```bash
# Basic signing with subject name
dotnet run sign document.pdf signed_document.pdf "CN=John Doe"

# Using thumbprint (most reliable method)
dotnet run sign document.pdf signed_document.pdf "A6B149D4A2C7D5F3C5E777640B6534652A674040"

# Using partial subject name
dotnet run sign invoice.pdf signed_invoice.pdf "MyCompany"

# With custom reason and location
dotnet run sign contract.pdf signed_contract.pdf "John Doe" "Contract approval" "New York Office"

# Thumbprint with spaces (also supported)
dotnet run sign document.pdf signed_document.pdf "A6 B1 49 D4 A2 C7 D5 F3 C5 E7 77 64 0B 65 34 65 2A 67 40 40"

# Batch sign multiple files (uses default location "PdfSigner by rysiok")
dotnet run batch "*.pdf" "signed_output" "localhost"
dotnet run batch "documents/*.pdf" "output" "John Doe" "Batch signed" "Office" "-approved"

# Verify a signed PDF
dotnet run verify signed_document.pdf
```

### Verify PDF Signatures

To verify the signatures in a signed PDF file:

```bash
# Verify all signatures in a PDF
dotnet run verify signed_document.pdf
```

This will:
- Check all signatures present in the PDF
- Verify signature integrity and authenticity
- Display certificate information including serial numbers
- Report whether signatures cover the whole document
- Show overall verification status

### Verification Features

- **Automatic Verification**: All signed PDFs are automatically verified after signing
- **SERIALNUMBER Validation**: **Requires** SERIALNUMBER property in certificate subject DN for verification
- **Serial Number Validation**: Compares the signing certificate SERIALNUMBER with the PDF signature
- **Integrity Check**: Validates that signatures cover the complete document
- **Multiple Signatures**: Handles PDFs with multiple signatures
- **Detailed Reporting**: Shows certificate details and verification status for each signature
- **Strict Requirements**: Verification fails if SERIALNUMBER property is missing from certificate
```

## Certificate Requirements

The application will look for certificates that:
1. Have a private key accessible to the current user
2. Are preferably valid (not expired)
3. **Must contain SERIALNUMBER property in subject DN** (e.g., `SERIALNUMBER=PNOPL-76091708291`)
4. Are located in either:
   - Current User Personal store (`CurrentUser\My`)
   - Local Machine Personal store (`LocalMachine\My`)

**Note**: Verification will fail if the certificate does not have a SERIALNUMBER property in its subject Distinguished Name.

## How It Works

1. **Certificate Discovery**: The application searches Windows Certificate Store using the provided subject name
2. **Certificate Validation**: It finds certificates with private keys and prefers valid (non-expired) ones
3. **PDF Processing**: Uses iText to read the input PDF and prepare for signing
4. **Digital Signing**: Applies the digital signature using BouncyCastle cryptographic operations
5. **Signature Verification**: Automatically verifies the signature and validates serial number matching
6. **Output Generation**: Saves the signed PDF to the specified output path

## Security Notes

- The application requires access to private keys in the certificate store
- Ensure you have proper permissions to access the certificates
- The signed PDF will contain the certificate's public key and signature
- Private keys never leave the Windows Certificate Store

## Dependencies

- **iText** (9.3.0): PDF manipulation library
- **iText.bouncy-castle-adapter** (9.3.0): Cryptographic operations
- **.NET 9.0**: Runtime framework

## Error Handling

The application handles common scenarios:
- Certificate not found in store
- Missing or invalid input files
- Insufficient permissions
- Invalid certificate (no private key, expired)
- PDF processing errors

## Troubleshooting

**Certificate not found:**
- Use the `list` command to see available certificates
- Try using a partial subject name instead of the full distinguished name
- Check if the certificate has a private key

**Access denied:**
- Ensure you have read access to the certificate store
- Try running as administrator for LocalMachine store access
- Check certificate permissions

**PDF signing fails:**
- Ensure the input PDF is not password-protected
- Check that the output directory exists and is writable
- Verify the certificate is valid and not expired

## Quick Start Scripts

For easier usage, you can use the provided batch script:

### Batch Script (Windows CMD)
```cmd
REM List certificates
sign.bat list

REM List certificates to file
sign.bat list -o certificates.txt

REM Sign a PDF using subject name
sign.bat sign document.pdf signed_document.pdf "localhost"

REM Sign using thumbprint with output log
sign.bat sign document.pdf signed_document.pdf "A6B149D4A2C7D5F3C5E777640B6534652A674040" -o sign_log.txt

REM Sign with custom reason and location
sign.bat sign contract.pdf signed_contract.pdf "John Doe" "Contract signature" "New York"

REM Batch sign multiple PDFs with results log
sign.bat batch "*.pdf" "signed" "localhost" --output batch_results.txt
sign.bat batch "documents\*.pdf" "output" "John Doe" "Batch processed" "Office" "-approved" -o batch_log.txt

REM Verify a signed PDF
sign.bat verify signed_document.pdf

REM Verify with results log
sign.bat verify signed_document.pdf --output verification.txt
```

The batch script includes:
- Automatic project building if needed
- Input validation and error checking
- .NET SDK availability verification
- Clear error messages and usage examples

## Building for Release

The project includes convenient build scripts for creating standalone executables:

### Using Build Scripts

**PowerShell:**
```powershell
.\build.ps1 release    # Build optimized release (~40MB, compressed)
.\build.ps1 debug      # Build debug version with symbols
.\build.ps1 both       # Build both release and debug
.\build.ps1 clean      # Clean all build outputs
```

**Batch File:**
```cmd
build.bat release      # Build optimized release
build.bat debug        # Build debug version
build.bat both         # Build both versions
build.bat clean        # Clean all outputs
```

### Output Locations
- **Release**: `bin\Release\net9.0\win-x64\publish\PdfSigner.exe` (~40MB)
- **Debug**: `bin\Debug\net9.0\win-x64\publish\PdfSigner.exe`

The release executable is:
- Self-contained (includes .NET 9.0 runtime)
- Single-file (no external dependencies)
- Compressed for smaller size
- Includes custom application icon
- Embeds version information (1.0.0)

### Manual Build Commands

To build a release version:

```bash
dotnet build -c Release
```

To publish as a single executable:

```bash
dotnet publish PdfSigner.csproj -c Release
```

The executable can be copied to any Windows x64 machine and run without installing .NET.

## Project Structure

```
PdfSigner/
├── PdfSigner.csproj          # Project file with dependencies and build config
├── Program.cs                # Main console application entry point
├── OutputWriter.cs           # Output abstraction for console/file redirection
├── WindowsCertificatePdfSigner.cs  # Core signing functionality
├── icon.ico                 # Application icon (blue document with signature)
├── build.ps1                # PowerShell build script
├── build.bat                # Windows CMD build script
├── create-icon.ps1          # Icon generation script
├── sign.bat                 # Windows CMD convenience script for signing
├── sample.pdf               # Sample PDF for testing
├── README.md               # This documentation
├── .github/
│   └── copilot-instructions.md  # AI coding agent guidelines
├── .vscode/                # VS Code configuration
│   ├── launch.json         # Debug configurations
│   └── tasks.json          # Build tasks
└── PdfSigner.Tests/        # Test project
    ├── TestAssemblyFixture.cs  # Automatic certificate cleanup
    ├── PdfSigningTests.cs      # Signing operation tests
    ├── PdfVerificationTests.cs # Verification tests
    ├── ErrorScenarioTests.cs   # Error handling tests
    └── Utilities/             # Test helpers
        ├── TestCertificateGenerator.cs
        └── TestPdfGenerator.cs
```

## Architecture

The application consists of main classes:

1. **WindowsCertificatePdfSigner**: Handles certificate discovery and PDF signing operations
2. **DotNetSignature**: Custom implementation of iText's IExternalSignature interface using .NET crypto classes
3. **OutputWriter**: Provides abstraction for output destinations (console or file) with automatic resource management

The design allows for easy extension to support additional certificate stores, signature algorithms, or output destinations.

## License

This project is provided as-is for educational and development purposes.