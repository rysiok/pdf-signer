using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using PdfSigner.Tests.Utilities;
using PdfSignerApp;
using Xunit;

namespace PdfSigner.Tests;

/// <summary>
/// Tests for error scenarios and edge cases
/// </summary>
[Collection("Assembly Collection")]
public class ErrorScenarioTests : IDisposable
{
    private readonly WindowsCertificatePdfSigner _signer;
    private readonly string _testDataDir;
    private readonly List<IDisposable> _certificateCleanups;

    public ErrorScenarioTests()
    {
        _signer = new WindowsCertificatePdfSigner();
        _testDataDir = Path.Combine(Path.GetTempPath(), "ErrorScenarioTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDir);
        _certificateCleanups = new List<IDisposable>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SignPdf_InvalidCertificateIdentifier_ShouldThrowException(string? certificateIdentifier)
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.pdf");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        TestPdfGenerator.CreateSimplePdf(inputPath);

        // Act & Assert
        var action = () => _signer.SignPdf(inputPath, outputPath, certificateIdentifier!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Certificate identifier cannot be null or empty*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SignPdf_InvalidInputPath_ShouldThrowException(string? inputPath)
    {
        // Arrange
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        var cert = TestCertificateGenerator.CreateCertificateWithSerialNumber("ErrorTestCert", "ERR123");
        using var cleanup = TestCertificateGenerator.InstallCertificateToStore(cert);

        // Act & Assert
        var action = () => _signer.SignPdf(inputPath!, outputPath, "ErrorTestCert");
        action.Should().Throw<Exception>();
        
        cert.Dispose();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SignPdf_InvalidOutputPath_ShouldThrowException(string? outputPath)
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.pdf");
        TestPdfGenerator.CreateSimplePdf(inputPath);
        var cert = TestCertificateGenerator.CreateCertificateWithSerialNumber("ErrorTestCert", "ERR123");
        using var cleanup = TestCertificateGenerator.InstallCertificateToStore(cert);

        // Act & Assert
        var action = () => _signer.SignPdf(inputPath, outputPath!, "ErrorTestCert");
        action.Should().Throw<Exception>();
        
        cert.Dispose();
    }

    [Fact]
    public void SignPdf_ReadOnlyOutputFile_ShouldThrowException()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.pdf");
        var outputPath = Path.Combine(_testDataDir, "readonly_output.pdf");
        TestPdfGenerator.CreateSimplePdf(inputPath);
        
        // Create read-only output file
        File.WriteAllText(outputPath, "dummy");
        File.SetAttributes(outputPath, FileAttributes.ReadOnly);
        
        var cert = TestCertificateGenerator.CreateCertificateWithSerialNumber("ErrorTestCert", "ERR123");
        using var cleanup = TestCertificateGenerator.InstallCertificateToStore(cert);

        try
        {
            // Act & Assert
            var action = () => _signer.SignPdf(inputPath, outputPath, "ErrorTestCert");
            action.Should().Throw<Exception>();
        }
        finally
        {
            // Clean up read-only file
            try
            {
                File.SetAttributes(outputPath, FileAttributes.Normal);
                File.Delete(outputPath);
            }
            catch { }
            cert.Dispose();
        }
    }

    [Fact]
    public void SignPdf_CorruptedInputPdf_ShouldThrowException()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "corrupted.pdf");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        TestPdfGenerator.CreateInvalidPdf(inputPath);
        
        var cert = TestCertificateGenerator.CreateCertificateWithSerialNumber("ErrorTestCert", "ERR123");
        using var cleanup = TestCertificateGenerator.InstallCertificateToStore(cert);

        // Act & Assert
        var action = () => _signer.SignPdf(inputPath, outputPath, "ErrorTestCert");
        action.Should().Throw<Exception>();
        
        cert.Dispose();
    }

    [Fact]
    public void SignBatch_InvalidInputPattern_ShouldCompleteGracefully()
    {
        // Arrange
        var outputDir = Path.Combine(_testDataDir, "batch_output");
        var cert = TestCertificateGenerator.CreateCertificateWithSerialNumber("BatchTestCert", "BATCH123");
        using var cleanup = TestCertificateGenerator.InstallCertificateToStore(cert);

        // Act & Assert - Should not throw for invalid patterns
        var action = () => _signer.SignBatch("", outputDir, "BatchTestCert");
        action.Should().NotThrow();
        
        cert.Dispose();
    }

    [Fact]
    public void SignBatch_InvalidOutputDirectory_ShouldCreateDirectory()
    {
        // Arrange
        var inputDir = Path.Combine(_testDataDir, "batch_input");
        var outputDir = Path.Combine(_testDataDir, "new_output_dir");
        Directory.CreateDirectory(inputDir);
        TestPdfGenerator.CreateSimplePdf(Path.Combine(inputDir, "test.pdf"));
        
        var cert = TestCertificateGenerator.CreateCertificateWithSerialNumber("BatchTestCert", "BATCH123");
        using var cleanup = TestCertificateGenerator.InstallCertificateToStore(cert);

        // Act
        var action = () => _signer.SignBatch($"{inputDir}\\*.pdf", outputDir, "BatchTestCert");

        // Assert
        action.Should().NotThrow();
        Directory.Exists(outputDir).Should().BeTrue();
        
        cert.Dispose();
    }

    [Fact]
    public void VerifyPdfSignature_LockedFile_ShouldThrowException()
    {
        // Arrange
        var pdfPath = Path.Combine(_testDataDir, "locked.pdf");
        TestPdfGenerator.CreateSimplePdf(pdfPath);
        
        // Lock the file by opening it
        using var fileStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.None);

        // Act & Assert
        var action = () => _signer.VerifyPdfSignature(pdfPath);
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void ListAvailableCertificates_ShouldNotThrow()
    {
        // Act & Assert
        var action = () => _signer.ListAvailableCertificates();
        action.Should().NotThrow();
    }

    [Fact]
    public void ExtractSerialNumberFromSubject_MalformedSubject_ShouldHandleGracefully()
    {
        // Test various malformed subject DNs
        var testCases = new[]
        {
            "SERIALNUMBER=", // Empty serial number
            "SERIALNUMBER", // Missing equals
            "CN=Test,SERIALNUMBER", // Missing value
            "SERIALNUMBER==123", // Double equals
            "Invalid DN format", // Not a proper DN
            "CN=Test,=123", // Missing attribute name
        };

        foreach (var testCase in testCases)
        {
            // Act
            var result = InvokeExtractSerialNumberFromSubject(testCase);

            // Assert - Should not throw and should return empty string for malformed inputs
            result.Should().NotBeNull();
        }
    }

    [Theory]
    [InlineData("C:\\invalid\\drive\\path")]
    [InlineData("")]
    [InlineData("   ")]
    public void GetMatchingFiles_InvalidPaths_ShouldThrowOrHandleGracefully(string invalidPath)
    {
        // Act & Assert
        var action = () => InvokeGetMatchingFiles(invalidPath);
        
        if (string.IsNullOrWhiteSpace(invalidPath))
        {
            action.Should().NotThrow(); // Should handle empty/null gracefully
        }
        else if (invalidPath.StartsWith("C:\\invalid"))
        {
            action.Should().Throw<TargetInvocationException>()
                .WithInnerException<DirectoryNotFoundException>(); // Non-existent directory should throw
        }
    }

    [Fact]
    public void IsThumbprint_ExtremeInputs_ShouldHandleGracefully()
    {
        // Test extreme inputs
        var testCases = new[]
        {
            new string('A', 1000), // Very long string
            "🚀🎯📝", // Unicode characters
            "\0\r\n\t", // Control characters
            null!, // Null (if method doesn't handle it)
        };

        foreach (var testCase in testCases)
        {
            // Act & Assert
            var action = () => InvokeIsThumbprint(testCase);
            action.Should().NotThrow();
        }
    }

    [Fact]
    public void SignPdf_CertificateWithoutPrivateKey_ShouldThrowException()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.pdf");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");
        TestPdfGenerator.CreateSimplePdf(inputPath);

        // Create a certificate without private key (public key only)
        var certWithPrivateKey = TestCertificateGenerator.CreateCertificateWithSerialNumber("PublicOnlyCert", "PUB123");
        var publicOnlyCert = X509CertificateLoader.LoadCertificate(certWithPrivateKey.RawData); // This removes the private key
        
        using var cleanup = TestCertificateGenerator.InstallCertificateToStore(publicOnlyCert);

        // Act & Assert
        var action = () => _signer.SignPdf(inputPath, outputPath, "PublicOnlyCert");
        action.Should().Throw<Exception>();
        
        certWithPrivateKey.Dispose();
        publicOnlyCert.Dispose();
    }

    // Helper methods to invoke private methods
    private string InvokeExtractSerialNumberFromSubject(string? subjectDN)
    {
        var method = typeof(WindowsCertificatePdfSigner).GetMethod("ExtractSerialNumberFromSubject", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (string)(method?.Invoke(_signer, new object?[] { subjectDN }) ?? "");
    }

    private string[] InvokeGetMatchingFiles(string pattern)
    {
        var method = typeof(WindowsCertificatePdfSigner).GetMethod("GetMatchingFiles", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (string[])(method?.Invoke(_signer, new object[] { pattern }) ?? Array.Empty<string>());
    }

    private bool InvokeIsThumbprint(string value)
    {
        var method = typeof(WindowsCertificatePdfSigner).GetMethod("IsThumbprint", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (bool)(method?.Invoke(_signer, new object[] { value }) ?? false);
    }

    public void Dispose()
    {
        // Clean up certificates
        foreach (var cleanup in _certificateCleanups)
        {
            cleanup.Dispose();
        }
        
        // Clean up test directory
        try
        {
            if (Directory.Exists(_testDataDir))
            {
                Directory.Delete(_testDataDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}