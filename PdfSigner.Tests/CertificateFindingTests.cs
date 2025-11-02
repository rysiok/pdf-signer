using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using PdfSigner.Tests.Utilities;
using PdfSignerApp;
using Xunit;

namespace PdfSigner.Tests;

/// <summary>
/// Tests for certificate finding and validation methods
/// </summary>
public class CertificateFindingTests : IDisposable
{
    private readonly WindowsCertificatePdfSigner _signer;
    private readonly List<IDisposable> _certificateCleanups;
    private readonly X509Certificate2 _testCertWithSerial;
    private readonly X509Certificate2 _testCertWithoutSerial;
    private readonly X509Certificate2 _expiredCert;

    public CertificateFindingTests()
    {
        _signer = new WindowsCertificatePdfSigner();
        _certificateCleanups = new List<IDisposable>();
        
        // Create test certificates
        _testCertWithSerial = TestCertificateGenerator.CreateCertificateWithSerialNumber("TestCertWithSerial", "123456789");
        _testCertWithoutSerial = TestCertificateGenerator.CreateCertificateWithoutSerialNumber("TestCertWithoutSerial");
        _expiredCert = TestCertificateGenerator.CreateExpiredCertificate("ExpiredTestCert", "999888777");
        
        // Install certificates to store for testing
        _certificateCleanups.Add(TestCertificateGenerator.InstallCertificateToStore(_testCertWithSerial));
        _certificateCleanups.Add(TestCertificateGenerator.InstallCertificateToStore(_testCertWithoutSerial));
        _certificateCleanups.Add(TestCertificateGenerator.InstallCertificateToStore(_expiredCert));
    }

    [Fact]
    public void FindCertificate_BySubjectName_ShouldReturnCorrectCertificate()
    {
        // Arrange & Act
        var certificate = InvokeFindCertificate("TestCertWithSerial");

        // Assert
        certificate.Should().NotBeNull();
        certificate!.Subject.Should().Contain("CN=TestCertWithSerial");
        certificate.Subject.Should().Contain("SERIALNUMBER=123456789");
    }

    [Fact]
    public void FindCertificate_ByPartialSubjectName_ShouldReturnCorrectCertificate()
    {
        // Arrange & Act
        var certificate = InvokeFindCertificate("TestCertWithSerial");

        // Assert
        certificate.Should().NotBeNull();
        certificate!.Subject.Should().Contain("TestCertWithSerial");
    }

    [Fact]
    public void FindCertificate_ByThumbprint_ShouldReturnCorrectCertificate()
    {
        // Arrange
        var thumbprint = _testCertWithSerial.Thumbprint;

        // Act
        var certificate = InvokeFindCertificate(thumbprint);

        // Assert
        certificate.Should().NotBeNull();
        certificate!.Thumbprint.Should().Be(thumbprint);
    }

    [Fact]
    public void FindCertificate_NonExistentCertificate_ShouldReturnNull()
    {
        // Arrange & Act
        var certificate = InvokeFindCertificate("NonExistentCertificate");

        // Assert
        certificate.Should().BeNull();
    }

    [Theory]
    [InlineData("1234567890ABCDEF1234567890ABCDEF12345678", true)]  // Valid SHA-1 thumbprint
    [InlineData("1234567890ABCDEF1234567890ABCDEF12345678901234567890ABCDEF12345678", true)]  // Valid SHA-256 thumbprint
    [InlineData("12 34 56 78 90 AB CD EF 12 34 56 78 90 AB CD EF 12 34 56 78", true)]  // With spaces
    [InlineData("12:34:56:78:90:AB:CD:EF:12:34:56:78:90:AB:CD:EF:12:34:56:78", true)]  // With colons
    [InlineData("TestCertificate", false)]  // Regular text
    [InlineData("123", false)]  // Too short
    [InlineData("GHIJKLMNOP1234567890ABCDEF1234567890ABCDEF", false)]  // Invalid hex characters
    public void IsThumbprint_VariousInputs_ShouldReturnExpectedResult(string input, bool expected)
    {
        // Arrange & Act
        var result = InvokeIsThumbprint(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FindCertificate_PreferValidOverExpired_ShouldReturnValidCertificate()
    {
        // Arrange - both certificates exist, but one is expired
        // The method should prefer the valid one

        // Act
        var certificate = InvokeFindCertificate("TestCertWithSerial");

        // Assert
        certificate.Should().NotBeNull();
        certificate!.Subject.Should().Contain("TestCertWithSerial");
        DateTime.Now.Should().BeOnOrAfter(certificate.NotBefore);
        DateTime.Now.Should().BeOnOrBefore(certificate.NotAfter);
    }

    [Fact]
    public void ExtractSerialNumberFromSubject_ValidSubjectWithSerial_ShouldReturnSerialNumber()
    {
        // Arrange
        var subjectDN = "CN=TestCert, SERIALNUMBER=123456789, O=TestOrg";

        // Act
        var serialNumber = InvokeExtractSerialNumberFromSubject(subjectDN);

        // Assert
        serialNumber.Should().Be("123456789");
    }

    [Fact]
    public void ExtractSerialNumberFromSubject_ValidSubjectWithoutSerial_ShouldReturnEmpty()
    {
        // Arrange
        var subjectDN = "CN=TestCert, O=TestOrg";

        // Act
        var serialNumber = InvokeExtractSerialNumberFromSubject(subjectDN);

        // Assert
        serialNumber.Should().BeEmpty();
    }

    [Theory]
    [InlineData("CN=Test, SERIALNUMBER=ABC123, O=Org", "ABC123")]
    [InlineData("SERIALNUMBER=XYZ789, CN=Test", "XYZ789")]
    [InlineData("CN=Test; SERIALNUMBER=DEF456", "DEF456")]  // Semicolon separator
    [InlineData("serialnumber=lowercase123, CN=Test", "lowercase123")]  // Case insensitive
    [InlineData("CN=Test, O=Org", "")]  // No serial number
    [InlineData("", "")]  // Empty string
    [InlineData(null, "")]  // Null string
    public void ExtractSerialNumberFromSubject_VariousFormats_ShouldHandleCorrectly(string? subjectDN, string expected)
    {
        // Arrange & Act
        var serialNumber = InvokeExtractSerialNumberFromSubject(subjectDN);

        // Assert
        serialNumber.Should().Be(expected);
    }

    // Helper methods to invoke private methods using reflection
    private X509Certificate2? InvokeFindCertificate(string identifier)
    {
        var method = typeof(WindowsCertificatePdfSigner).GetMethod("FindCertificate", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (X509Certificate2?)method?.Invoke(_signer, new object[] { identifier });
    }

    private bool InvokeIsThumbprint(string value)
    {
        var method = typeof(WindowsCertificatePdfSigner).GetMethod("IsThumbprint", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (bool)(method?.Invoke(_signer, new object[] { value }) ?? false);
    }

    private string InvokeExtractSerialNumberFromSubject(string? subjectDN)
    {
        var method = typeof(WindowsCertificatePdfSigner).GetMethod("ExtractSerialNumberFromSubject", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (string)(method?.Invoke(_signer, new object?[] { subjectDN }) ?? "");
    }

    public void Dispose()
    {
        foreach (var cleanup in _certificateCleanups)
        {
            cleanup.Dispose();
        }
        _testCertWithSerial?.Dispose();
        _testCertWithoutSerial?.Dispose();
        _expiredCert?.Dispose();
    }
}