using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace PdfSigner.Tests.Utilities;

/// <summary>
/// Helper class for creating test certificates
/// </summary>
public static class TestCertificateGenerator
{
    /// <summary>
    /// Creates a self-signed certificate with SERIALNUMBER property in the subject
    /// </summary>
    /// <param name="subjectName">Base subject name (CN=)</param>
    /// <param name="serialNumber">Serial number to include in subject DN</param>
    /// <param name="validDays">Number of days the certificate is valid</param>
    /// <returns>X509Certificate2 with private key</returns>
    public static X509Certificate2 CreateCertificateWithSerialNumber(
        string subjectName = "TestCert", 
        string serialNumber = "123456789", 
        int validDays = 365)
    {
        var subject = $"CN={subjectName}, SERIALNUMBER={serialNumber}";
        return CreateSelfSignedCertificate(subject, validDays);
    }

    /// <summary>
    /// Creates a self-signed certificate without SERIALNUMBER property in the subject
    /// </summary>
    /// <param name="subjectName">Subject name (CN=)</param>
    /// <param name="validDays">Number of days the certificate is valid</param>
    /// <returns>X509Certificate2 with private key</returns>
    public static X509Certificate2 CreateCertificateWithoutSerialNumber(
        string subjectName = "TestCertNoSerial", 
        int validDays = 365)
    {
        var subject = $"CN={subjectName}";
        return CreateSelfSignedCertificate(subject, validDays);
    }

    /// <summary>
    /// Creates an expired certificate with SERIALNUMBER
    /// </summary>
    /// <param name="subjectName">Subject name</param>
    /// <param name="serialNumber">Serial number</param>
    /// <returns>Expired X509Certificate2</returns>
    public static X509Certificate2 CreateExpiredCertificate(
        string subjectName = "ExpiredTestCert", 
        string serialNumber = "987654321")
    {
        var subject = $"CN={subjectName}, SERIALNUMBER={serialNumber}";
        return CreateSelfSignedCertificate(subject, validDays: -10, startDaysAgo: -20);
    }

    /// <summary>
    /// Creates a certificate with a specific thumbprint pattern (for testing thumbprint detection)
    /// </summary>
    /// <param name="subjectName">Subject name</param>
    /// <param name="serialNumber">Serial number</param>
    /// <returns>X509Certificate2 with predictable thumbprint</returns>
    public static X509Certificate2 CreateCertificateForThumbprintTest(
        string subjectName = "ThumbprintTestCert", 
        string serialNumber = "ABCDEF123")
    {
        var subject = $"CN={subjectName}, SERIALNUMBER={serialNumber}";
        return CreateSelfSignedCertificate(subject, 365);
    }

    /// <summary>
    /// Installs a certificate to the Windows certificate store for testing
    /// </summary>
    /// <param name="certificate">Certificate to install</param>
    /// <param name="storeLocation">Store location (CurrentUser or LocalMachine)</param>
    /// <param name="storeName">Store name (typically My for personal certificates)</param>
    /// <returns>Disposable that removes the certificate when disposed</returns>
    public static IDisposable InstallCertificateToStore(
        X509Certificate2 certificate,
        StoreLocation storeLocation = StoreLocation.CurrentUser,
        StoreName storeName = StoreName.My)
    {
        var store = new X509Store(storeName, storeLocation);
        store.Open(OpenFlags.ReadWrite);
        store.Add(certificate);
        store.Close();

        return new CertificateStoreCleanup(certificate, storeLocation, storeName);
    }

    private static X509Certificate2 CreateSelfSignedCertificate(
        string subject, 
        int validDays, 
        int startDaysAgo = 0)
    {
        // Generate key pair
        var keyPairGenerator = new RsaKeyPairGenerator();
        keyPairGenerator.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
        var keyPair = keyPairGenerator.GenerateKeyPair();

        // Certificate generator
        var certificateGenerator = new X509V3CertificateGenerator();
        
        // Serial number
        var serialNumber = BigInteger.ProbablePrime(120, new Random());
        certificateGenerator.SetSerialNumber(serialNumber);

        // Validity period
        var notBefore = DateTime.UtcNow.AddDays(startDaysAgo);
        var notAfter = notBefore.AddDays(validDays);
        certificateGenerator.SetNotBefore(notBefore);
        certificateGenerator.SetNotAfter(notAfter);

        // Subject and issuer (self-signed)
        var subjectDN = new X509Name(subject);
        certificateGenerator.SetSubjectDN(subjectDN);
        certificateGenerator.SetIssuerDN(subjectDN);

        // Public key
        certificateGenerator.SetPublicKey(keyPair.Public);

        // Extensions
        certificateGenerator.AddExtension(
            X509Extensions.BasicConstraints,
            false,
            new Org.BouncyCastle.Asn1.X509.BasicConstraints(false));

        certificateGenerator.AddExtension(
            X509Extensions.KeyUsage,
            true,
            new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment));

        // Sign the certificate
        ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA256WithRSA", keyPair.Private, new SecureRandom());
        var bouncyCastleCert = certificateGenerator.Generate(signatureFactory);

        // Convert to .NET X509Certificate2
        var certBytes = bouncyCastleCert.GetEncoded();
        var cert = new X509Certificate2(certBytes);

        // Convert private key
        var rsaPrivateKey = DotNetUtilities.ToRSA((RsaPrivateCrtKeyParameters)keyPair.Private);
        var certWithPrivateKey = cert.CopyWithPrivateKey(rsaPrivateKey);

        return certWithPrivateKey;
    }

    private class CertificateStoreCleanup : IDisposable
    {
        private readonly X509Certificate2 _certificate;
        private readonly StoreLocation _storeLocation;
        private readonly StoreName _storeName;
        private bool _disposed = false;

        public CertificateStoreCleanup(X509Certificate2 certificate, StoreLocation storeLocation, StoreName storeName)
        {
            _certificate = certificate;
            _storeLocation = storeLocation;
            _storeName = storeName;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    var store = new X509Store(_storeName, _storeLocation);
                    store.Open(OpenFlags.ReadWrite);
                    store.Remove(_certificate);
                    store.Close();
                }
                catch
                {
                    // Ignore cleanup errors
                }
                _disposed = true;
            }
        }
    }
}