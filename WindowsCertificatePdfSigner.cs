using System;
using System.Collections.Generic;
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
            if (string.IsNullOrWhiteSpace(certificateSubject))
            {
                throw new ArgumentException("Certificate identifier cannot be null or empty.", nameof(certificateSubject));
            }
            
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
            
            // Verify the signature (skip if certificate doesn't have SERIALNUMBER)
            Console.WriteLine("Verifying signature...");
            try
            {
                var verificationResult = VerifySignature(outputPath, certificate);
                Console.WriteLine($"  ✓ SERIALNUMBER verified: {verificationResult.SigningCertificateSerialNumber}");
                Console.WriteLine("  ✓ Signature verified and authenticated");
                Console.WriteLine("✓ PDF signed and verified successfully");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("SERIALNUMBER property not found"))
            {
                // Certificate doesn't have SERIALNUMBER in subject - signing succeeded but verification limited
                Console.WriteLine("  ⚠ Warning: Certificate does not have SERIALNUMBER property - verification skipped");
                Console.WriteLine("✓ PDF signed successfully (verification skipped)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ PDF signed but verification failed: {ex.Message}");
                throw new InvalidOperationException("Signature verification failed", ex);
            }
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
            string[] inputFiles;
            try
            {
                inputFiles = GetMatchingFiles(inputPattern);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding files: {ex.Message}");
                return;
            }
            
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
                    
                    // Verify the signature
                    try
                    {
                        var verificationResult = VerifySignature(outputFile, certificate);
                        successCount++;
//                        Console.WriteLine($"  ✓ SERIALNUMBER verified: {verificationResult.SigningCertificateSerialNumber}");
//                        Console.WriteLine($"  ✓ Signature verified and authenticated");
                        Console.WriteLine($"{Path.GetFileName(inputFile)} -> {outputFileName} - status: signed and verified");
                    }
                    catch (Exception verifyEx)
                    {
                        failureCount++;
                        Console.WriteLine($"{Path.GetFileName(inputFile)} -> {outputFileName} - status: signed but verification failed ({verifyEx.Message})");
                    }
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
                    throw new DirectoryNotFoundException($"Directory not found: {directory}");
                }
            }
            catch (Exception ex) when (!(ex is DirectoryNotFoundException))
            {
                throw new InvalidOperationException($"Error searching for files: {ex.Message}", ex);
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
            if (string.IsNullOrWhiteSpace(value))
                return false;
                
            // Remove any spaces or colons that might be in the thumbprint
            var cleanValue = value.Replace(" ", "").Replace(":", "");
            
            // Check if it's a hex string of reasonable length (32-40 chars for SHA-1, 64+ for SHA-256)
            // Allow slightly longer strings as some systems may have extended formats
            return cleanValue.Length >= 32 && cleanValue.Length <= 128 && 
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
        /// <exception cref="InvalidOperationException">Thrown when certificate store access fails</exception>
        public void ListAvailableCertificates()
        {
            Console.WriteLine("Available certificates in Current User Personal store:");
            try
            {
                ListCertificatesInStore(StoreLocation.CurrentUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error accessing Current User store: {ex.Message}");
            }
            
            Console.WriteLine("\nAvailable certificates in Local Machine Personal store:");
            try
            {
                ListCertificatesInStore(StoreLocation.LocalMachine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error accessing Local Machine store: {ex.Message}");
            }
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
                throw new InvalidOperationException($"Error accessing certificate store {location}: {ex.Message}", ex);
            }
            finally
            {
                store.Close();
            }
        }

        /// <summary>
        /// Verifies the signature of a signed PDF file without requiring the original signing certificate
        /// </summary>
        /// <param name="signedPdfPath">Path to the signed PDF file</param>
        /// <returns>Verification result with details</returns>
        /// <exception cref="InvalidOperationException">Thrown when PDF verification fails</exception>
        /// <exception cref="FileNotFoundException">Thrown when the PDF file is not found</exception>
        public PdfVerificationResult VerifyPdfSignature(string signedPdfPath)
        {
            if (!File.Exists(signedPdfPath))
            {
                throw new FileNotFoundException($"PDF file not found: {signedPdfPath}");
            }

            try
            {
                using var reader = new PdfReader(signedPdfPath);
                using var document = new PdfDocument(reader);
                
                var signatureUtil = new SignatureUtil(document);
                var signatureNames = signatureUtil.GetSignatureNames();
                
                if (signatureNames.Count == 0)
                {
                    throw new InvalidOperationException("No signatures found in the PDF");
                }

                var signatures = new List<SignatureInfo>();
                bool allValid = true;
                
                for (int i = 0; i < signatureNames.Count; i++)
                {
                    var signatureName = signatureNames[i];
                    var signatureInfo = new SignatureInfo { Name = signatureName };
                    
                    try
                    {
                        var pkcs7 = signatureUtil.ReadSignatureData(signatureName);
                        
                        if (pkcs7 == null)
                        {
                            throw new InvalidOperationException($"Could not read signature data for {signatureName}");
                        }

                        // Verify signature integrity
                        var signatureCoversWholeDocument = signatureUtil.SignatureCoversWholeDocument(signatureName);
                        if (!signatureCoversWholeDocument)
                        {
                            throw new InvalidOperationException($"Signature {signatureName} does not cover the whole document");
                        }

                        // Get the signing certificate from the PDF
                        var signingCerts = pkcs7.GetSignCertificateChain();
                        if (signingCerts == null || signingCerts.Length == 0)
                        {
                            throw new InvalidOperationException($"No signing certificate found in signature {signatureName}");
                        }

                        var pdfSigningCert = signingCerts[0];
                        
                        // Get certificate subject and extract SERIALNUMBER
                        var certSubject = pdfSigningCert.GetSubjectDN()?.ToString() ?? "Unknown";
                        var certSerialNumber = ExtractSerialNumberFromSubject(certSubject);
                        
                        // SERIALNUMBER property is required for verification
                        if (string.IsNullOrEmpty(certSerialNumber))
                        {
                            throw new InvalidOperationException($"SERIALNUMBER property not found in certificate subject for signature {signatureName} - verification failed");
                        }

                        // Verify the signature cryptographically
                        var isValid = pkcs7.VerifySignatureIntegrityAndAuthenticity();
                        if (!isValid)
                        {
                            throw new InvalidOperationException($"Signature integrity verification failed for {signatureName}");
                        }

                        signatureInfo.IsValid = true;
                        signatureInfo.CertificateSubject = certSubject;
                        signatureInfo.SerialNumber = certSerialNumber;
                    }
                    catch (Exception ex)
                    {
                        signatureInfo.IsValid = false;
                        signatureInfo.ErrorMessage = ex.Message;
                        allValid = false;
                    }
                    
                    signatures.Add(signatureInfo);
                }
                
                return new PdfVerificationResult 
                { 
                    IsValid = allValid, 
                    Signatures = signatures,
                    TotalSignatures = signatureNames.Count
                };
            }
            catch (Exception ex) when (!(ex is InvalidOperationException || ex is FileNotFoundException))
            {
                throw new InvalidOperationException($"PDF verification failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extracts the SERIALNUMBER property from a certificate subject DN
        /// </summary>
        /// <param name="subjectDN">The certificate subject distinguished name</param>
        /// <returns>The SERIALNUMBER value if found, empty string otherwise</returns>
        private string ExtractSerialNumberFromSubject(string subjectDN)
        {
            if (string.IsNullOrEmpty(subjectDN))
                return "";

            // Try different separators and formats
            // Windows certificate store might use different formats: comma, semicolon, or newline separated
            var separators = new[] { ',', ';', '\n', '\r' };
            var parts = subjectDN.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                // Check for various formats: SERIALNUMBER=, OID.2.5.4.5=, or SN=
                if (trimmedPart.StartsWith("SERIALNUMBER=", StringComparison.OrdinalIgnoreCase) ||
                    trimmedPart.StartsWith("OID.2.5.4.5=", StringComparison.OrdinalIgnoreCase) ||
                    trimmedPart.StartsWith("SN=", StringComparison.OrdinalIgnoreCase))
                {
                    var equalsIndex = trimmedPart.IndexOf('=');
                    if (equalsIndex > 0 && equalsIndex < trimmedPart.Length - 1)
                    {
                        return trimmedPart.Substring(equalsIndex + 1).Trim();
                    }
                }
            }
            
            return "";
        }

        /// <summary>
        /// Verifies the signature of a signed PDF file
        /// </summary>
        /// <param name="signedPdfPath">Path to the signed PDF file</param>
        /// <param name="signingCertificate">The certificate that was used for signing</param>
        /// <returns>Verification result with details</returns>
        /// <exception cref="InvalidOperationException">Thrown when signature verification fails</exception>
        private SignatureVerificationResult VerifySignature(string signedPdfPath, X509Certificate2 signingCertificate)
        {
            try
            {
                using var reader = new PdfReader(signedPdfPath);
                using var document = new PdfDocument(reader);
                
                var signatureUtil = new SignatureUtil(document);
                var signatureNames = signatureUtil.GetSignatureNames();
                
                if (signatureNames.Count == 0)
                {
                    throw new InvalidOperationException("No signatures found in the PDF");
                }

                // Extract SERIALNUMBER from the signing certificate for comparison
                var signingCertSerialNumber = ExtractSerialNumberFromSubject(signingCertificate.Subject);
                
                // SERIALNUMBER property is required for verification
                if (string.IsNullOrEmpty(signingCertSerialNumber))
                {
                    throw new InvalidOperationException("SERIALNUMBER property not found in signing certificate subject - verification failed");
                }

                // Check all signatures to find the one matching our certificate
                List<string> checkedSignatures = new List<string>();
                foreach (var signatureName in signatureNames)
                {
                    var pkcs7 = signatureUtil.ReadSignatureData(signatureName);
                    
                    if (pkcs7 == null)
                    {
                        checkedSignatures.Add($"{signatureName}: Could not read signature data");
                        continue;
                    }

                    // Get the signing certificate from the PDF
                    var signingCerts = pkcs7.GetSignCertificateChain();
                    if (signingCerts == null || signingCerts.Length == 0)
                    {
                        checkedSignatures.Add($"{signatureName}: No signing certificate found");
                        continue;
                    }

                    var pdfSigningCert = signingCerts[0];
                    var pdfCertSubject = pdfSigningCert.GetSubjectDN()?.ToString() ?? "";
                    var pdfCertSerialNumber = ExtractSerialNumberFromSubject(pdfCertSubject);
                    
                    // Check if this signature matches our certificate
                    if (string.IsNullOrEmpty(pdfCertSerialNumber))
                    {
                        checkedSignatures.Add($"{signatureName}: No SERIALNUMBER in certificate");
                        continue;
                    }
                    
                    if (signingCertSerialNumber != pdfCertSerialNumber)
                    {
                        checkedSignatures.Add($"{signatureName}: SERIALNUMBER mismatch (expected: {signingCertSerialNumber}, found: {pdfCertSerialNumber})");
                        continue;
                    }
                    
                    // Found matching signature! Verify it
                    var signatureCoversWholeDocument = signatureUtil.SignatureCoversWholeDocument(signatureName);
                    if (!signatureCoversWholeDocument)
                    {
                        throw new InvalidOperationException($"Signature '{signatureName}' does not cover the whole document");
                    }

                    // Verify the signature cryptographically
                    var isValid = pkcs7.VerifySignatureIntegrityAndAuthenticity();
                    if (!isValid)
                    {
                        throw new InvalidOperationException($"Signature '{signatureName}' integrity verification failed");
                    }

                    // Success!
                    return new SignatureVerificationResult
                    {
                        IsValid = true,
                        SigningCertificateSerialNumber = signingCertSerialNumber,
                        PdfCertificateSerialNumber = pdfCertSerialNumber,
                        CertificateSubject = pdfCertSubject,
                        Message = $"SERIALNUMBER verified: {signingCertSerialNumber}"
                    };
                }
                
                // No matching signature found
                var checkedInfo = string.Join("; ", checkedSignatures);
                throw new InvalidOperationException($"No signature found matching certificate with SERIALNUMBER '{signingCertSerialNumber}'. Checked signatures: {checkedInfo}");
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Verification failed: {ex.Message}", ex);
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
            
            // Use append mode to preserve existing signatures
            var stampingProperties = new StampingProperties();
            stampingProperties.UseAppendMode();
            
            var signer = new iText.Signatures.PdfSigner(reader, writer, stampingProperties);

            // Create certificate using the raw certificate data
            var bcCert = new X509CertificateBC(new Org.BouncyCastle.X509.X509CertificateParser().ReadCertificate(certificate.RawData));
            var chain = new IX509Certificate[] { bcCert };

            // Create external signature using .NET crypto classes directly
            IExternalSignature externalSignature = new DotNetSignature(certificate, "SHA256");

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

    /// <summary>
    /// Result of PDF signature verification
    /// </summary>
    public class PdfVerificationResult
    {
        public bool IsValid { get; set; }
        public int TotalSignatures { get; set; }
        public List<SignatureInfo> Signatures { get; set; } = new List<SignatureInfo>();
    }

    /// <summary>
    /// Information about a single signature in a PDF
    /// </summary>
    public class SignatureInfo
    {
        public string Name { get; set; } = "";
        public bool IsValid { get; set; }
        public string CertificateSubject { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of signature verification during signing process
    /// </summary>
    public class SignatureVerificationResult
    {
        public bool IsValid { get; set; }
        public string SigningCertificateSerialNumber { get; set; } = "";
        public string PdfCertificateSerialNumber { get; set; } = "";
        public string CertificateSubject { get; set; } = "";
        public string Message { get; set; } = "";
    }
}