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
            ShowSignUsage();
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  PdfSigner.exe list");
            Console.WriteLine("  PdfSigner.exe sign document.pdf signed_document.pdf \"CN=John Doe\"");
            Console.WriteLine("  PdfSigner.exe sign document.pdf signed_document.pdf \"John Doe\" \"Contract signature\" \"New York\"");
            Console.WriteLine("  PdfSigner.exe sign document.pdf signed_document.pdf \"A6B149D4A2C7D5F3C5E777640B6534652A674040\"");
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
    }
}