using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using iText.Kernel.Pdf;
using iText.Signatures;
using iText.Commons.Bouncycastle.Cert;
using iText.Commons.Bouncycastle.Crypto;
using iText.Bouncycastle.X509;
using iText.Bouncycastle.Crypto;

namespace PdfSignerApp
{
    public class WindowsCertificatePdfSigner
    {
        /// <summary>
        /// Signs a PDF file using a certificate from the Windows certificate store
        /// </summary>
        /// <param name="inputPath">Path to the input PDF file</param>
        /// <param name="outputPath">Path to save the signed PDF file</param>
        /// <param name="certificateSubject">Subject name, partial subject name, or thumbprint to find the certificate</param>
        /// <param name="reason">Reason for signing (optional)</param>
        /// <param name="location">Location of signing (optional)</param>
        public void SignPdf(string inputPath, string outputPath, string certificateSubject, 
                           string reason = "Document signed", string location = "")
        {
            // Find certificate in Windows certificate store
            var certificate = FindCertificate(certificateSubject);
            if (certificate == null)
            {
                throw new InvalidOperationException($"Certificate with identifier '{certificateSubject}' not found in certificate store.");
            }

            Console.WriteLine($"Found certificate: {certificate.Subject}");
            Console.WriteLine($"Valid from: {certificate.NotBefore} to {certificate.NotAfter}");

            // Sign the PDF
            SignPdfWithCertificate(inputPath, outputPath, certificate, reason, location);
        }

        /// <summary>
        /// Signs multiple PDF files using a certificate from the Windows certificate store
        /// </summary>
        /// <param name="inputPattern">Pattern to match input PDF files (e.g., "*.pdf", "folder/*.pdf")</param>
        /// <param name="outputDirectory">Directory where signed PDF files will be saved</param>
        /// <param name="certificateSubject">Subject name, partial subject name, or thumbprint to find the certificate</param>
        /// <param name="reason">Reason for signing (optional)</param>
        /// <param name="location">Location of signing (optional)</param>
        /// <param name="outputSuffix">Suffix to add to output filenames (optional, default: "-sig")</param>
        public void SignBatch(string inputPattern, string outputDirectory, string certificateSubject, 
                             string reason = "Document signed", string location = "", string outputSuffix = "-sig")
        {
            // Find certificate in Windows certificate store
            var certificate = FindCertificate(certificateSubject);
            if (certificate == null)
            {
                throw new InvalidOperationException($"Certificate with identifier '{certificateSubject}' not found in certificate store.");
            }

            Console.WriteLine($"Found certificate: {certificate.Subject}");
            Console.WriteLine($"Valid from: {certificate.NotBefore} to {certificate.NotAfter}");
            Console.WriteLine();

            // Create output directory if it doesn't exist
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
                Console.WriteLine($"Created output directory: {outputDirectory}");
            }

            // Find matching PDF files
            var inputFiles = GetMatchingFiles(inputPattern);
            
            if (inputFiles.Length == 0)
            {
                Console.WriteLine($"No PDF files found matching pattern: {inputPattern}");
                return;
            }

            Console.WriteLine($"Found {inputFiles.Length} PDF file(s) to sign");
            Console.WriteLine();

            // Sign each file
            int successCount = 0;
            int failureCount = 0;

            foreach (var inputFile in inputFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(inputFile);
                    var extension = Path.GetExtension(inputFile);
                    var outputFileName = $"{fileName}{outputSuffix}{extension}";
                    var outputFile = Path.Combine(outputDirectory, outputFileName);
                    
                    SignPdfWithCertificate(inputFile, outputFile, certificate, reason, location);
                    
                    successCount++;
                    Console.WriteLine($"{Path.GetFileName(inputFile)} -> {outputFileName} - status: signed");
                }
                catch (Exception ex)
                {
                    failureCount++;
                    Console.WriteLine($"{Path.GetFileName(inputFile)} -> {Path.GetFileName(inputFile)} - status: failed ({ex.Message})");
                }
            }

            // Summary
            Console.WriteLine("Batch signing completed:");
            Console.WriteLine($"  ✓ Successful: {successCount}");
            Console.WriteLine($"  ✗ Failed: {failureCount}");
            Console.WriteLine($"  📁 Output directory: {outputDirectory}");
        }

        /// <summary>
        /// Gets files matching the specified pattern
        /// </summary>
        /// <param name="pattern">File pattern (supports wildcards and directory paths)</param>
        /// <returns>Array of matching file paths</returns>
        private string[] GetMatchingFiles(string pattern)
        {
            try
            {
                // Handle different pattern types
                if (File.Exists(pattern))
                {
                    // Single file path
                    return new[] { pattern };
                }
                
                // Extract directory and search pattern
                var directory = Path.GetDirectoryName(pattern);
                var searchPattern = Path.GetFileName(pattern);

                // Use current directory if no directory specified
                if (string.IsNullOrEmpty(directory))
                {
                    directory = Directory.GetCurrentDirectory();
                }

                // Default to *.pdf if no pattern specified
                if (string.IsNullOrEmpty(searchPattern))
                {
                    searchPattern = "*.pdf";
                }

                // Ensure we're looking for PDF files
                if (!searchPattern.Contains('.'))
                {
                    searchPattern += "*.pdf";
                }

                // Search for files
                if (Directory.Exists(directory))
                {
                    return Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly)
                                  .Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                                  .OrderBy(f => f)
                                  .ToArray();
                }
                else
                {
                    Console.WriteLine($"Directory not found: {directory}");
                    return new string[0];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching for files: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Finds a certificate in the Windows certificate store by subject name or thumbprint
        /// </summary>
        /// <param name="identifier">Subject name, partial subject name, or thumbprint to search for</param>
        /// <returns>X509Certificate2 if found, null otherwise</returns>
        private X509Certificate2? FindCertificate(string identifier)
        {
            // Check if identifier looks like a thumbprint (hex string, typically 40 characters for SHA-1)
            bool isThumbprint = IsThumbprint(identifier);
            
            // Search in Current User Personal store first
            var certificate = FindCertificateInStore(StoreLocation.CurrentUser, identifier, isThumbprint);
            if (certificate != null)
                return certificate;

            // Search in Local Machine Personal store if not found in Current User
            return FindCertificateInStore(StoreLocation.LocalMachine, identifier, isThumbprint);
        }

        /// <summary>
        /// Determines if a string looks like a certificate thumbprint
        /// </summary>
        /// <param name="value">The string to check</param>
        /// <returns>True if it looks like a thumbprint, false otherwise</returns>
        private bool IsThumbprint(string value)
        {
            // Remove any spaces or colons that might be in the thumbprint
            var cleanValue = value.Replace(" ", "").Replace(":", "");
            
            // Check if it's a hex string of reasonable length (typically 40 chars for SHA-1, 64 for SHA-256)
            return cleanValue.Length >= 32 && cleanValue.Length <= 64 && 
                   System.Text.RegularExpressions.Regex.IsMatch(cleanValue, @"^[0-9A-Fa-f]+$");
        }

        /// <summary>
        /// Finds a certificate in a specific store location
        /// </summary>
        /// <param name="location">Store location to search</param>
        /// <param name="identifier">Subject name or thumbprint</param>
        /// <param name="isThumbprint">Whether the identifier is a thumbprint</param>
        /// <returns>X509Certificate2 if found, null otherwise</returns>
        private X509Certificate2? FindCertificateInStore(StoreLocation location, string identifier, bool isThumbprint)
        {
            var store = new X509Store(StoreName.My, location);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certificates;

                if (isThumbprint)
                {
                    // Search by thumbprint
                    var cleanThumbprint = identifier.Replace(" ", "").Replace(":", "");
                    certificates = store.Certificates.Find(X509FindType.FindByThumbprint, cleanThumbprint, false);
                }
                else
                {
                    // Search by subject name (exact match first)
                    certificates = store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, identifier, false);
                    
                    if (certificates.Count == 0)
                    {
                        // Try partial subject name search
                        certificates = store.Certificates.Find(X509FindType.FindBySubjectName, identifier, false);
                    }
                }

                if (certificates.Count > 0)
                {
                    // Return the first valid certificate with a private key
                    foreach (X509Certificate2 cert in certificates)
                    {
                        if (cert.HasPrivateKey && DateTime.Now >= cert.NotBefore && DateTime.Now <= cert.NotAfter)
                        {
                            return cert;
                        }
                    }
                    
                    // If no valid certificate found, return the first one with private key
                    foreach (X509Certificate2 cert in certificates)
                    {
                        if (cert.HasPrivateKey)
                        {
                            return cert;
                        }
                    }
                }
            }
            finally
            {
                store.Close();
            }

            return null;
        }

        /// <summary>
        /// Lists all available certificates in the Windows certificate store
        /// </summary>
        public void ListAvailableCertificates()
        {
            Console.WriteLine("Available certificates in Current User Personal store:");
            ListCertificatesInStore(StoreLocation.CurrentUser);
            
            Console.WriteLine("\nAvailable certificates in Local Machine Personal store:");
            ListCertificatesInStore(StoreLocation.LocalMachine);
        }

        private void ListCertificatesInStore(StoreLocation location)
        {
            var store = new X509Store(StoreName.My, location);
            try
            {
                store.Open(OpenFlags.ReadOnly);
                foreach (X509Certificate2 cert in store.Certificates)
                {
                    Console.WriteLine($"  Subject: {cert.Subject}");
                    Console.WriteLine($"  Issuer: {cert.Issuer}");
                    Console.WriteLine($"  Serial: {cert.SerialNumber}");
                    Console.WriteLine($"  Valid: {cert.NotBefore} to {cert.NotAfter}");
                    Console.WriteLine($"  Has Private Key: {cert.HasPrivateKey}");
                    Console.WriteLine($"  Thumbprint: {cert.Thumbprint}");
                    Console.WriteLine("  ---");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error accessing store: {ex.Message}");
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// Signs the PDF file with the provided certificate
        /// </summary>
        private void SignPdfWithCertificate(string inputPath, string outputPath, 
                                          X509Certificate2 certificate,
                                          string reason, string location)
        {
            using var reader = new PdfReader(inputPath);
            using var writer = new PdfWriter(outputPath);
            
            var signer = new iText.Signatures.PdfSigner(reader, writer, new StampingProperties());

            // Create certificate using the raw certificate data
            var bcCert = new X509CertificateBC(new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(certificate.RawData));
            var chain = new IX509Certificate[] { bcCert };

            // Create external signature using .NET crypto classes directly
            IExternalSignature externalSignature = new DotNetSignature(certificate, DigestAlgorithms.SHA256);

            // Sign the document
            signer.SignDetached(externalSignature, chain, null, null, null, 0, iText.Signatures.PdfSigner.CryptoStandard.CMS);
        }
    }

    /// <summary>
    /// Custom signature implementation that uses .NET crypto classes
    /// </summary>
    public class DotNetSignature : IExternalSignature
    {
        private readonly X509Certificate2 _certificate;
        private readonly string _digestAlgorithm;

        public DotNetSignature(X509Certificate2 certificate, string digestAlgorithm)
        {
            _certificate = certificate;
            _digestAlgorithm = digestAlgorithm;
        }

        public string GetDigestAlgorithmName()
        {
            return _digestAlgorithm;
        }

        public string GetSignatureAlgorithmName()
        {
            return _digestAlgorithm.ToUpper() + "with" + GetKeyAlgorithm();
        }

        private string GetKeyAlgorithm()
        {
            if (_certificate.GetRSAPrivateKey() != null)
                return "RSA";
            else if (_certificate.GetECDsaPrivateKey() != null)
                return "ECDSA";
            else
                throw new NotSupportedException("Unsupported key algorithm");
        }

        public byte[] Sign(byte[] message)
        {
            try
            {
                if (_certificate.GetRSAPrivateKey() is RSA rsa)
                {
                    return rsa.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
                else if (_certificate.GetECDsaPrivateKey() is ECDsa ecdsa)
                {
                    return ecdsa.SignData(message, HashAlgorithmName.SHA256);
                }
                else
                {
                    throw new NotSupportedException("Unsupported private key algorithm");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to sign data: {ex.Message}", ex);
            }
        }

        public ISignatureMechanismParams? GetSignatureMechanismParameters()
        {
            return null;
        }
    }
}