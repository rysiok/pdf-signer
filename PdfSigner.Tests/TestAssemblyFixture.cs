using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace PdfSigner.Tests;

/// <summary>
/// Assembly-level fixture that runs once before all tests to ensure clean state
/// </summary>
public class TestAssemblyFixture : IDisposable
{
    public TestAssemblyFixture()
    {
        // Clean up any orphaned test certificates before running tests
        CleanupOrphanedTestCertificates();
    }

    /// <summary>
    /// Removes all test certificates from the Windows Certificate Store.
    /// This handles cases where previous test runs were interrupted or failed.
    /// </summary>
    private static void CleanupOrphanedTestCertificates()
    {
        try
        {
            var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);

            var certificatesToRemove = store.Certificates
                .Cast<X509Certificate2>()
                .Where(cert => IsTestCertificate(cert))
                .ToList();

            foreach (var cert in certificatesToRemove)
            {
                store.Remove(cert);
            }

            store.Close();
        }
        catch
        {
            // Ignore cleanup errors - if we can't clean up, tests will still run
            // Individual test fixtures will install their own certificates
        }
    }

    /// <summary>
    /// Determines if a certificate is a test certificate based on subject patterns
    /// </summary>
    private static bool IsTestCertificate(X509Certificate2 cert)
    {
        var subject = cert.Subject;
        
        // Match SERIALNUMBER patterns
        if (subject.Contains("SERIALNUMBER=ERR123") ||
            subject.Contains("SERIALNUMBER=BATCH123") ||
            subject.Contains("SERIALNUMBER=PUB123") ||
            subject.Contains("SERIALNUMBER=SIGN123456") ||
            subject.Contains("SERIALNUMBER=SECOND987654") ||
            subject.Contains("SERIALNUMBER=SECOND789") ||
            subject.Contains("SERIALNUMBER=DIFFERENT123"))
        {
            return true;
        }

        // Match CN patterns (for certificates without SERIALNUMBER)
        if (subject.Contains("CN=SigningTestCert") ||
            subject.Contains("CN=SecondSignerCert") ||
            subject.Contains("CN=ErrorTestCert") ||
            subject.Contains("CN=BatchTestCert") ||
            subject.Contains("CN=PublicOnlyCert") ||
            subject.Contains("CN=AnotherCert") ||
            subject.Contains("CN=SecondCert"))
        {
            return true;
        }

        return false;
    }

    public void Dispose()
    {
        // Final cleanup after all tests complete
        CleanupOrphanedTestCertificates();
    }
}

/// <summary>
/// Collection definition to ensure the assembly fixture is used by all test classes
/// </summary>
[CollectionDefinition("Assembly Collection")]
public class AssemblyCollection : ICollectionFixture<TestAssemblyFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionFixture<>] and all the
    // ICollectionFixture<> interfaces.
}
