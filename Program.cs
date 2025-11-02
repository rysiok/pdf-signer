using System;
using System.IO;

namespace PdfSignerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PDF Signer using Windows Certificate Store");
            Console.WriteLine("=========================================");

            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            var signer = new WindowsCertificatePdfSigner();

            try
            {
                switch (args[0].ToLower())
                {
                    case "list":
                        signer.ListAvailableCertificates();
                        break;

                    case "sign":
                        if (args.Length < 4)
                        {
                            Console.WriteLine("Error: Missing required parameters for signing.");
                            ShowSignUsage();
                            return;
                        }

                        var inputFile = args[1];
                        var outputFile = args[2];
                        var certificateSubject = args[3];
                        var reason = args.Length > 4 ? args[4] : "Document digitally signed";
                        var location = args.Length > 5 ? args[5] : Environment.MachineName;

                        if (!File.Exists(inputFile))
                        {
                            Console.WriteLine($"Error: Input file '{inputFile}' not found.");
                            return;
                        }

                        Console.WriteLine($"Signing PDF: {inputFile}");
                        Console.WriteLine($"Output file: {outputFile}");
                        Console.WriteLine($"Certificate identifier: {certificateSubject}");
                        Console.WriteLine($"Reason: {reason}");
                        Console.WriteLine($"Location: {location}");
                        Console.WriteLine();

                        signer.SignPdf(inputFile, outputFile, certificateSubject, reason, location);
                        break;

                    case "batch":
                        if (args.Length < 4)
                        {
                            Console.WriteLine("Error: Missing required parameters for batch signing.");
                            ShowBatchUsage();
                            return;
                        }

                        var inputPattern = args[1];
                        var outputDirectory = args[2];
                        var batchCertificateSubject = args[3];
                        var batchReason = args.Length > 4 ? args[4] : "Document digitally signed";
                        var batchLocation = args.Length > 5 ? args[5] : Environment.MachineName;
                        var outputSuffix = args.Length > 6 ? args[6] : "-sig";

                        Console.WriteLine($"Batch signing PDFs:");
                        Console.WriteLine($"Input pattern: {inputPattern}");
                        Console.WriteLine($"Output directory: {outputDirectory}");
                        Console.WriteLine($"Certificate identifier: {batchCertificateSubject}");
                        Console.WriteLine($"Reason: {batchReason}");
                        Console.WriteLine($"Location: {batchLocation}");
                        Console.WriteLine($"Output suffix: {outputSuffix}");
                        Console.WriteLine();

                        signer.SignBatch(inputPattern, outputDirectory, batchCertificateSubject, batchReason, batchLocation, outputSuffix);
                        break;

                    case "verify":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("Error: Missing required parameter for verification.");
                            ShowVerifyUsage();
                            return;
                        }

                        var pdfToVerify = args[1];

                        if (!File.Exists(pdfToVerify))
                        {
                            Console.WriteLine($"Error: PDF file '{pdfToVerify}' not found.");
                            return;
                        }

                        Console.WriteLine($"Verifying PDF: {pdfToVerify}");
                        Console.WriteLine();

                        var isVerified = signer.VerifyPdfSignature(pdfToVerify);
                        Console.WriteLine();
                        Console.WriteLine(isVerified ? "✓ PDF signature verification successful" : "✗ PDF signature verification failed");
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {args[0]}");
                        ShowUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  PdfSigner.exe list");
            Console.WriteLine("    - Lists all available certificates in Windows certificate store");
            Console.WriteLine();
            Console.WriteLine("  PdfSigner.exe sign <input.pdf> <output.pdf> <certificate_identifier> [reason] [location]");
            Console.WriteLine("    - Signs a PDF file using a certificate from Windows certificate store");
            Console.WriteLine();
            Console.WriteLine("  PdfSigner.exe batch <input_pattern> <output_directory> <certificate_identifier> [reason] [location] [suffix]");
            Console.WriteLine("    - Signs multiple PDF files matching a pattern");
            Console.WriteLine();
            Console.WriteLine("  PdfSigner.exe verify <signed.pdf>");
            Console.WriteLine("    - Verifies the signature of a signed PDF file");
            Console.WriteLine();
            ShowSignUsage();
            Console.WriteLine();
            ShowBatchUsage();
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  PdfSigner.exe list");
            Console.WriteLine("  PdfSigner.exe sign document.pdf signed_document.pdf \"CN=John Doe\"");
            Console.WriteLine("  PdfSigner.exe sign document.pdf signed_document.pdf \"John Doe\" \"Contract signature\" \"New York\"");
            Console.WriteLine("  PdfSigner.exe sign document.pdf signed_document.pdf \"A6B149D4A2C7D5F3C5E777640B6534652A674040\"");
            Console.WriteLine("  PdfSigner.exe batch \"*.pdf\" \"signed\" \"localhost\"");
            Console.WriteLine("  PdfSigner.exe batch \"documents/*.pdf\" \"output\" \"John Doe\" \"Batch signed\" \"Office\" \"-approved\"");
            Console.WriteLine("  PdfSigner.exe verify signed_document.pdf");
        }

        static void ShowSignUsage()
        {
            Console.WriteLine("Sign parameters:");
            Console.WriteLine("  <input.pdf>              - Path to the PDF file to sign");
            Console.WriteLine("  <output.pdf>             - Path where the signed PDF will be saved");
            Console.WriteLine("  <certificate_identifier> - Certificate identifier: subject name, partial name, or thumbprint");
            Console.WriteLine("                             Examples:");
            Console.WriteLine("                             - 'CN=John Doe' (full distinguished name)");
            Console.WriteLine("                             - 'John Doe' (partial subject name)");
            Console.WriteLine("                             - 'A6B149D4A2C7D5F3C5E777640B6534652A674040' (thumbprint)");
            Console.WriteLine("  [reason]                 - Optional: Reason for signing (default: 'Document digitally signed')");
            Console.WriteLine("  [location]               - Optional: Location of signing (default: computer name)");
        }

        static void ShowBatchUsage()
        {
            Console.WriteLine("Batch sign parameters:");
            Console.WriteLine("  <input_pattern>          - Pattern to match PDF files:");
            Console.WriteLine("                             - '*.pdf' (all PDFs in current directory)");
            Console.WriteLine("                             - 'folder/*.pdf' (all PDFs in specific folder)");
            Console.WriteLine("                             - 'documents/contract*.pdf' (matching pattern)");
            Console.WriteLine("  <output_directory>       - Directory where signed PDFs will be saved");
            Console.WriteLine("  <certificate_identifier> - Certificate identifier (same as single sign)");
            Console.WriteLine("  [reason]                 - Optional: Reason for signing (default: 'Document digitally signed')");
            Console.WriteLine("  [location]               - Optional: Location of signing (default: computer name)");
            Console.WriteLine("  [suffix]                 - Optional: Suffix for output filenames (default: '-sig')");
        }

        static void ShowVerifyUsage()
        {
            Console.WriteLine("Verify parameters:");
            Console.WriteLine("  <signed.pdf>             - Path to the signed PDF file to verify");
        }
    }
}