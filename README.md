# PDF Signer with Windows Certificate Store

This C# console application allows you to digitally sign PDF files using certificates from the Windows Certificate Store. It uses the iText7 library for PDF manipulation and BouncyCastle for cryptographic operations.

## Features

- **Certificate Discovery**: Automatically finds certificates from Windows Certificate Store (both Current User and Local Machine stores)
- **PDF Signing**: Signs PDF files with digital signatures using X.509 certificates
- **Certificate Listing**: Lists all available certificates with their details
- **Command Line Interface**: Easy-to-use CLI for batch operations
- **Error Handling**: Comprehensive error handling and validation

## Prerequisites

- .NET 8.0 or later
- Windows operating system (for certificate store access)
- Valid X.509 certificate with private key in Windows Certificate Store

## Installation

1. Clone or download the project
2. Navigate to the project directory
3. Restore dependencies and build:
   ```bash
   dotnet restore
   dotnet build
   ```

## Usage

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
dotnet run sign <input.pdf> <output.pdf> <certificate_identifier> [reason] [location]
```

**Parameters:**
- `<input.pdf>`: Path to the PDF file you want to sign
- `<output.pdf>`: Path where the signed PDF will be saved
- `<certificate_identifier>`: Certificate identifier - can be:
  - **Subject name**: Full distinguished name (e.g., "CN=John Doe, O=Company")
  - **Partial subject name**: Part of the subject (e.g., "John Doe" or "localhost")
  - **Thumbprint**: Certificate thumbprint (e.g., "A6B149D4A2C7D5F3C5E777640B6534652A674040")
- `[reason]` (optional): Reason for signing (default: "Document digitally signed")
- `[location]` (optional): Location of signing (default: computer name)

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
```

## Certificate Requirements

The application will look for certificates that:
1. Have a private key accessible to the current user
2. Are preferably valid (not expired)
3. Are located in either:
   - Current User Personal store (`CurrentUser\My`)
   - Local Machine Personal store (`LocalMachine\My`)

## How It Works

1. **Certificate Discovery**: The application searches Windows Certificate Store using the provided subject name
2. **Certificate Validation**: It finds certificates with private keys and prefers valid (non-expired) ones
3. **PDF Processing**: Uses iText7 to read the input PDF and prepare for signing
4. **Digital Signing**: Applies the digital signature using BouncyCastle cryptographic operations
5. **Output Generation**: Saves the signed PDF to the specified output path

## Security Notes

- The application requires access to private keys in the certificate store
- Ensure you have proper permissions to access the certificates
- The signed PDF will contain the certificate's public key and signature
- Private keys never leave the Windows Certificate Store

## Dependencies

- **iText7** (8.0.2): PDF manipulation library
- **iText7.bouncy-castle-adapter** (8.0.2): Cryptographic operations
- **.NET 8.0**: Runtime framework

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

REM Sign a PDF using subject name
sign.bat sign document.pdf signed_document.pdf "localhost"

REM Sign using thumbprint
sign.bat sign document.pdf signed_document.pdf "A6B149D4A2C7D5F3C5E777640B6534652A674040"

REM Sign with custom reason and location
sign.bat sign contract.pdf signed_contract.pdf "John Doe" "Contract signature" "New York"
```

The batch script includes:
- Automatic project building if needed
- Input validation and error checking
- .NET SDK availability verification
- Clear error messages and usage examples

## Building for Release

To build a release version:

```bash
dotnet build -c Release
```

To publish as a single executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Project Structure

```
PdfSigner/
├── PdfSigner.csproj          # Project file with dependencies
├── Program.cs                # Main console application entry point
├── WindowsCertificatePdfSigner.cs  # Core signing functionality
├── sample.pdf               # Sample PDF for testing
├── sign.bat                 # Windows CMD batch script for easy usage
├── README.md               # This documentation
└── .vscode/                # VS Code configuration
    ├── launch.json         # Debug configurations
    └── tasks.json          # Build tasks
```

## Architecture

The application consists of two main classes:

1. **WindowsCertificatePdfSigner**: Handles certificate discovery and PDF signing operations
2. **DotNetSignature**: Custom implementation of iText's IExternalSignature interface using .NET crypto classes

The design allows for easy extension to support additional certificate stores or signature algorithms.

## License

This project is provided as-is for educational and development purposes.