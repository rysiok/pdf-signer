using System;
using System.IO;
using System.Linq;

namespace PdfSignerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Parse output file option
            string? outputFile = null;
            var filteredArgs = args.ToList();
            
            for (int i = 0; i < filteredArgs.Count; i++)
            {
                if ((filteredArgs[i] == "--output" || filteredArgs[i] == "-o") && i + 1 < filteredArgs.Count)
                {
                    outputFile = filteredArgs[i + 1];
                    filteredArgs.RemoveAt(i); // Remove --output
                    filteredArgs.RemoveAt(i); // Remove the file path
                    break;
                }
            }

            args = filteredArgs.ToArray();

            using var output = new OutputWriter(outputFile);

            output.WriteLine("PDF Signer using Windows Certificate Store");
            output.WriteLine("=========================================");

            if (args.Length == 0)
            {
                ShowUsage(output);
                return;
            }

            var signer = new WindowsCertificatePdfSigner();

            try
            {
                switch (args[0].ToLower())
                {
                    case "list":
                        signer.ListAvailableCertificates(output);
                        break;

                    case "sign":
                        if (args.Length < 4)
                        {
                            output.WriteLine("Error: Missing required parameters for signing.");
                            ShowSignUsage(output);
                            return;
                        }

                        SignPdf(signer, args, output);
                        break;

                    case "batch":
                        if (args.Length < 4)
                        {
                            output.WriteLine("Error: Missing required parameters for batch signing.");
                            ShowBatchUsage(output);
                            return;
                        }

                        BatchSignPdf(signer, args, output);
                        break;

                    case "verify":
                        if (args.Length < 2)
                        {
                            output.WriteLine("Error: Missing required parameter for verification.");
                            ShowVerifyUsage(output);
                            return;
                        }

                        VerifyPdf(signer, args, output);
                        break;

                    default:
                        output.WriteLine($"Unknown command: {args[0]}");
                        ShowUsage(output);
                        break;
                }
            }
            catch (Exception ex)
            {
                output.WriteLine($"Error: {ex.Message}");
                output.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        static void SignPdf(WindowsCertificatePdfSigner signer, string[] args, OutputWriter output)
        {
            var inputFile = args[1];
            var outputFilePath = args[2];
            var certificateSubject = args[3];
            var reason = args.Length > 4 ? args[4] : "Document digitally signed";
            var location = args.Length > 5 ? args[5] : "PdfSigner by rysiok";

            if (!File.Exists(inputFile))
            {
                output.WriteLine($"Error: Input file '{inputFile}' not found.");
                return;
            }

            output.WriteLine($"Signing PDF: {inputFile}");
            output.WriteLine($"Output file: {outputFilePath}");
            output.WriteLine($"Certificate identifier: {certificateSubject}");
            output.WriteLine($"Reason: {reason}");
            output.WriteLine($"Location: {location}");
            output.WriteLine();

            signer.SignPdf(inputFile, outputFilePath, certificateSubject, reason, location, output);
        }

        static void BatchSignPdf(WindowsCertificatePdfSigner signer, string[] args, OutputWriter output)
        {
            var inputPattern = args[1];
            var outputDirectory = args[2];
            var batchCertificateSubject = args[3];
            var batchReason = args.Length > 4 ? args[4] : "Document digitally signed";
            var batchLocation = args.Length > 5 ? args[5] : "PdfSigner by rysiok";
            var outputSuffix = args.Length > 6 ? args[6] : "-sig";

            output.WriteLine($"Batch signing PDFs:");
            output.WriteLine($"Input pattern: {inputPattern}");
            output.WriteLine($"Output directory: {outputDirectory}");
            output.WriteLine($"Certificate identifier: {batchCertificateSubject}");
            output.WriteLine($"Reason: {batchReason}");
            output.WriteLine($"Location: {batchLocation}");
            output.WriteLine($"Output suffix: {outputSuffix}");
            output.WriteLine();

            signer.SignBatch(inputPattern, outputDirectory, batchCertificateSubject, batchReason, batchLocation, outputSuffix, output);
        }

        static void VerifyPdf(WindowsCertificatePdfSigner signer, string[] args, OutputWriter output)
        {
            var pdfToVerify = args[1];

            if (!File.Exists(pdfToVerify))
            {
                output.WriteLine($"Error: PDF file '{pdfToVerify}' not found.");
                return;
            }

            output.WriteLine($"Verifying PDF: {pdfToVerify}");
            output.WriteLine();

            try
            {
                var verificationResult = signer.VerifyPdfSignature(pdfToVerify);
                
                output.WriteLine($"Found {verificationResult.TotalSignatures} signature(s)");
                
                for (int i = 0; i < verificationResult.Signatures.Count; i++)
                {
                    var sig = verificationResult.Signatures[i];
                    output.WriteLine($"\nVerifying signature {i + 1}: {sig.Name}");
                    
                    if (sig.IsValid)
                    {
                        output.WriteLine($"  ✓ Signature valid");
                        output.WriteLine($"  ✓ Certificate: {sig.CertificateSubject}");
                        if (!string.IsNullOrEmpty(sig.SerialNumber))
                        {
                            output.WriteLine($"  ✓ SERIALNUMBER: {sig.SerialNumber}");
                        }
                        else
                        {
                            output.WriteLine($"  ✓ SERIALNUMBER: Not present in certificate subject");
                        }
                    }
                    else
                    {
                        output.WriteLine($"  ✗ {sig.ErrorMessage}");
                    }
                }
                
                output.WriteLine();
                output.WriteLine(verificationResult.IsValid ? "✓ PDF signature verification successful" : "✗ PDF signature verification failed");
            }
            catch (Exception ex)
            {
                output.WriteLine($"✗ Verification failed: {ex.Message}");
            }
        }

        static void ShowUsage(OutputWriter output)
        {
            output.WriteLine("Usage:");
            output.WriteLine("  PdfSigner.exe list [--output <file>]");
            output.WriteLine("    - Lists all available certificates in Windows certificate store");
            output.WriteLine();
            output.WriteLine("  PdfSigner.exe sign <input.pdf> <output.pdf> <certificate_identifier> [reason] [location] [--output <file>]");
            output.WriteLine("    - Signs a PDF file using a certificate from Windows certificate store");
            output.WriteLine();
            output.WriteLine("  PdfSigner.exe batch <input_pattern> <output_directory> <certificate_identifier> [reason] [location] [suffix] [--output <file>]");
            output.WriteLine("    - Signs multiple PDF files matching a pattern");
            output.WriteLine();
            output.WriteLine("  PdfSigner.exe verify <signed.pdf> [--output <file>]");
            output.WriteLine("    - Verifies the signature of a signed PDF file");
            output.WriteLine();
            output.WriteLine("Global options:");
            output.WriteLine("  --output <file>, -o <file> - Write output to file instead of console");
            output.WriteLine();
            ShowSignUsage(output);
            output.WriteLine();
            ShowBatchUsage(output);
            output.WriteLine();
            output.WriteLine("Examples:");
            output.WriteLine("  PdfSigner.exe list");
            output.WriteLine("  PdfSigner.exe list --output certificates.txt");
            output.WriteLine("  PdfSigner.exe sign document.pdf signed_document.pdf \"CN=John Doe\"");
            output.WriteLine("  PdfSigner.exe sign document.pdf signed_document.pdf \"John Doe\" \"Contract signature\" \"New York\"");
            output.WriteLine("  PdfSigner.exe sign document.pdf signed_document.pdf \"A6B149D4A2C7D5F3C5E777640B6534652A674040\"");
            output.WriteLine("  PdfSigner.exe batch \"*.pdf\" \"signed\" \"localhost\"");
            output.WriteLine("  PdfSigner.exe batch \"documents/*.pdf\" \"output\" \"John Doe\" \"Batch signed\" \"Office\" \"-approved\" -o batch_log.txt");
            output.WriteLine("  PdfSigner.exe verify signed_document.pdf");
            output.WriteLine("  PdfSigner.exe verify signed_document.pdf --output verification_result.txt");
        }

        static void ShowSignUsage(OutputWriter output)
        {
            output.WriteLine("Sign parameters:");
            output.WriteLine("  <input.pdf>              - Path to the PDF file to sign");
            output.WriteLine("  <output.pdf>             - Path where the signed PDF will be saved");
            output.WriteLine("  <certificate_identifier> - Certificate identifier: subject name, partial name, or thumbprint");
            output.WriteLine("                             Examples:");
            output.WriteLine("                             - 'CN=John Doe' (full distinguished name)");
            output.WriteLine("                             - 'John Doe' (partial subject name)");
            output.WriteLine("                             - 'A6B149D4A2C7D5F3C5E777640B6534652A674040' (thumbprint)");
            output.WriteLine("  [reason]                 - Optional: Reason for signing (default: 'Document digitally signed')");
            output.WriteLine("  [location]               - Optional: Location of signing (default: 'PdfSigner by rysiok')");
        }

        static void ShowBatchUsage(OutputWriter output)
        {
            output.WriteLine("Batch sign parameters:");
            output.WriteLine("  <input_pattern>          - Pattern to match PDF files:");
            output.WriteLine("                             - '*.pdf' (all PDFs in current directory)");
            output.WriteLine("                             - 'folder/*.pdf' (all PDFs in specific folder)");
            output.WriteLine("                             - 'documents/contract*.pdf' (matching pattern)");
            output.WriteLine("  <output_directory>       - Directory where signed PDFs will be saved");
            output.WriteLine("  <certificate_identifier> - Certificate identifier (same as single sign)");
            output.WriteLine("  [reason]                 - Optional: Reason for signing (default: 'Document digitally signed')");
            output.WriteLine("  [location]               - Optional: Location of signing (default: 'PdfSigner by rysiok')");
            output.WriteLine("  [suffix]                 - Optional: Suffix for output filenames (default: '-sig')");
        }

        static void ShowVerifyUsage(OutputWriter output)
        {
            output.WriteLine("Verify parameters:");
            output.WriteLine("  <signed.pdf>             - Path to the signed PDF file to verify");
        }
    }
}