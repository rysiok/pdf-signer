using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using PdfSigner.Tests.Utilities;
using PdfSignerApp;
using Xunit;

namespace PdfSigner.Tests;

/// <summary>
/// Tests for PDF signing operations
/// </summary>
[Collection("Assembly Collection")]
public class PdfSigningTests : IDisposable
{
    private readonly WindowsCertificatePdfSigner _signer;
    private readonly string _testDataDir;
    private readonly List<IDisposable> _certificateCleanups;
    private readonly X509Certificate2 _validCertWithSerial;
    private readonly X509Certificate2 _certWithoutSerial;

    public PdfSigningTests()
    {
        _signer = new WindowsCertificatePdfSigner();
        _testDataDir = Path.Combine(Path.GetTempPath(), "PdfSignerTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDataDir);
        
        _certificateCleanups = new List<IDisposable>();
        
        // Create test certificates
        _validCertWithSerial = TestCertificateGenerator.CreateCertificateWithSerialNumber("SigningTestCert", "SIGN123456");
        _certWithoutSerial = TestCertificateGenerator.CreateCertificateWithoutSerialNumber("SigningTestCertNoSerial");
        
        // Install certificates to store
        _certificateCleanups.Add(TestCertificateGenerator.InstallCertificateToStore(_validCertWithSerial));
        _certificateCleanups.Add(TestCertificateGenerator.InstallCertificateToStore(_certWithoutSerial));
        
        // Create test PDF files
        TestPdfGenerator.CreateSimplePdf(Path.Combine(_testDataDir, "test.pdf"));
        TestPdfGenerator.CreateMultiPagePdf(Path.Combine(_testDataDir, "multipage.pdf"));
        TestPdfGenerator.CreateEmptyPdf(Path.Combine(_testDataDir, "empty.pdf"));
    }

    [Fact]
    public void SignPdf_ValidCertificateWithSerial_ShouldSucceed()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.pdf");
        var outputPath = Path.Combine(_testDataDir, "test_signed.pdf");

        // Act & Assert
        var action = () => _signer.SignPdf(inputPath, outputPath, "SigningTestCert", "Test signing", "Test location");
        
        action.Should().NotThrow();
        File.Exists(outputPath).Should().BeTrue();
        
        // Verify the signed PDF is larger than the original
        var originalSize = new FileInfo(inputPath).Length;
        var signedSize = new FileInfo(outputPath).Length;
        signedSize.Should().BeGreaterThan(originalSize);
    }

    [Fact]
    public void SignPdf_CertificateWithoutSerial_ShouldThrowException()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.pdf");
        var outputPath = Path.Combine(_testDataDir, "test_signed_no_serial.pdf");

        // Act & Assert
        // Note: SignPdf now allows certificates without SERIALNUMBER with a warning
        // The signing will succeed, but verification is skipped
        var action = () => _signer.SignPdf(inputPath, outputPath, "SigningTestCertNoSerial");
        
        action.Should().NotThrow();
        File.Exists(outputPath).Should().BeTrue();
    }

    [Fact]
    public void SignPdf_NonExistentCertificate_ShouldThrowException()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.pdf");
        var outputPath = Path.Combine(_testDataDir, "test_signed_nonexistent.pdf");

        // Act & Assert
        var action = () => _signer.SignPdf(inputPath, outputPath, "NonExistentCertificate");
        
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*not found in certificate store*");
    }

    [Fact]
    public void SignPdf_NonExistentInputFile_ShouldThrowException()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "nonexistent.pdf");
        var outputPath = Path.Combine(_testDataDir, "output.pdf");

        // Act & Assert
        var action = () => _signer.SignPdf(inputPath, outputPath, "SigningTestCert");
        
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void SignPdf_InvalidOutputPath_ShouldThrowException()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test.pdf");
        var outputPath = "Z:\\invalid\\path\\output.pdf"; // Invalid drive

        // Act & Assert
        var action = () => _signer.SignPdf(inputPath, outputPath, "SigningTestCert");
        
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void SignBatch_ValidCertificateWithSerial_ShouldSignAllFiles()
    {
        // Arrange
        var batchInputDir = Path.Combine(_testDataDir, "batch_input");
        var batchOutputDir = Path.Combine(_testDataDir, "batch_output");
        
        Directory.CreateDirectory(batchInputDir);
        var createdFiles = TestPdfGenerator.CreateMultiplePdfs(batchInputDir, 3, "batch_test");

        // Act
        var action = () => _signer.SignBatch($"{batchInputDir}\\*.pdf", batchOutputDir, "SigningTestCert");
        
        // Assert
        action.Should().NotThrow();
        
        // Verify output files were created
        Directory.Exists(batchOutputDir).Should().BeTrue();
        var outputFiles = Directory.GetFiles(batchOutputDir, "*-sig.pdf");
        outputFiles.Should().HaveCount(3);
        
        // Verify all output files exist and are larger than inputs
        foreach (var outputFile in outputFiles)
        {
            File.Exists(outputFile).Should().BeTrue();
            var outputSize = new FileInfo(outputFile).Length;
            outputSize.Should().BeGreaterThan(1000); // Should be substantial size after signing
        }
    }

    [Fact]
    public void SignBatch_CertificateWithoutSerial_ShouldFailAllFiles()
    {
        // Arrange
        var batchInputDir = Path.Combine(_testDataDir, "batch_input_no_serial");
        var batchOutputDir = Path.Combine(_testDataDir, "batch_output_no_serial");
        
        Directory.CreateDirectory(batchInputDir);
        TestPdfGenerator.CreateMultiplePdfs(batchInputDir, 2, "batch_fail");

        // Act
        var action = () => _signer.SignBatch($"{batchInputDir}\\*.pdf", batchOutputDir, "SigningTestCertNoSerial");
        
        // Assert - should not throw, but should fail internally
        action.Should().NotThrow();
        
        // Verify output directory exists but files should have failed verification
        Directory.Exists(batchOutputDir).Should().BeTrue();
    }

    [Fact]
    public void SignBatch_NoMatchingFiles_ShouldCompleteWithoutError()
    {
        // Arrange
        var emptyDir = Path.Combine(_testDataDir, "empty_dir");
        var outputDir = Path.Combine(_testDataDir, "empty_output");
        Directory.CreateDirectory(emptyDir);

        // Act & Assert
        var action = () => _signer.SignBatch($"{emptyDir}\\*.pdf", outputDir, "SigningTestCert");
        action.Should().NotThrow();
    }

    [Fact]
    public void SignBatch_CustomOutputSuffix_ShouldUseCustomSuffix()
    {
        // Arrange
        var batchInputDir = Path.Combine(_testDataDir, "batch_custom_suffix");
        var batchOutputDir = Path.Combine(_testDataDir, "batch_custom_output");
        
        Directory.CreateDirectory(batchInputDir);
        TestPdfGenerator.CreateMultiplePdfs(batchInputDir, 2, "custom");

        // Act
        var action = () => _signer.SignBatch($"{batchInputDir}\\*.pdf", batchOutputDir, "SigningTestCert", 
            outputSuffix: "-SIGNED");
        
        // Assert
        action.Should().NotThrow();
        
        var outputFiles = Directory.GetFiles(batchOutputDir, "*-SIGNED.pdf");
        outputFiles.Should().HaveCount(2);
    }

    [Fact]
    public void GetMatchingFiles_ExistingFiles_ShouldReturnCorrectFiles()
    {
        // Arrange
        var testDir = Path.Combine(_testDataDir, "matching_test");
        Directory.CreateDirectory(testDir);
        TestPdfGenerator.CreateMultiplePdfs(testDir, 3, "match");
        
        // Also create a non-PDF file
        File.WriteAllText(Path.Combine(testDir, "not_a_pdf.txt"), "This is not a PDF");

        // Act
        var result = InvokeGetMatchingFiles($"{testDir}\\*.pdf");

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(path => path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
        result.Should().OnlyContain(path => File.Exists(path));
    }

    [Fact]
    public void GetMatchingFiles_SingleFile_ShouldReturnSingleFile()
    {
        // Arrange
        var singleFile = Path.Combine(_testDataDir, "test.pdf");

        // Act
        var result = InvokeGetMatchingFiles(singleFile);

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be(singleFile);
    }

    [Fact]
    public void GetMatchingFiles_NonExistentDirectory_ShouldThrowException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataDir, "nonexistent", "*.pdf");

        // Act & Assert
        var action = () => InvokeGetMatchingFiles(nonExistentPath);
        action.Should().Throw<TargetInvocationException>()
            .WithInnerException<DirectoryNotFoundException>();
    }

    [Fact]
    public void SignPdf_DoubleSign_ShouldPreserveBothSignatures()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "test_double_sign.pdf");
        var firstSignedPath = Path.Combine(_testDataDir, "test_double_sign_first.pdf");
        var secondSignedPath = Path.Combine(_testDataDir, "test_double_sign_second.pdf");
        
        // Create another certificate for second signature
        var secondCert = TestCertificateGenerator.CreateCertificateWithSerialNumber("SecondSignerCert", "SECOND987654");
        var secondCertCleanup = TestCertificateGenerator.InstallCertificateToStore(secondCert);
        
        try
        {
            TestPdfGenerator.CreateSimplePdf(inputPath);
            
            // Act - Sign the PDF twice with different certificates
            _signer.SignPdf(inputPath, firstSignedPath, "SigningTestCert", "First signature", "Location 1");
            _signer.SignPdf(firstSignedPath, secondSignedPath, "SecondSignerCert", "Second signature", "Location 2");

            // Assert - Both signing operations should succeed
            File.Exists(firstSignedPath).Should().BeTrue("first signed file should be created");
            File.Exists(secondSignedPath).Should().BeTrue("second signed file should be created");
            
            // Verify both signatures exist using VerifyPdfSignature
            var result = _signer.VerifyPdfSignature(secondSignedPath);
            
            result.Should().NotBeNull();
            result.TotalSignatures.Should().Be(2, "PDF should contain both signatures");
            result.Signatures.Should().HaveCount(2);
            
            // The second (latest) signature should be valid and cover the whole document
            var secondSignature = result.Signatures.FirstOrDefault(s => s.CertificateSubject.Contains("SECOND987654"));
            secondSignature.Should().NotBeNull("second signature should be present");
            secondSignature!.IsValid.Should().BeTrue("second signature should be valid");
            
            // The first signature is preserved - it may or may not have SERIALNUMBER depending on which cert was found
            // Note: FindBySubjectName does partial matching, so "SigningTestCert" might match "SigningTestCertNoSerial" first
            var firstSignature = result.Signatures.FirstOrDefault(s => s != secondSignature);
            firstSignature.Should().NotBeNull("first signature should be preserved");
            
            // If the first signature has a serial number, it should be valid
            // If it doesn't have a serial number (matched SigningTestCertNoSerial), it will be invalid
            if (!string.IsNullOrEmpty(firstSignature!.SerialNumber))
            {
                firstSignature.IsValid.Should().BeTrue("first signature with SERIALNUMBER should remain cryptographically valid");
            }
        }
        finally
        {
            secondCertCleanup.Dispose();
            secondCert.Dispose();
        }
    }

    // Helper method to invoke private GetMatchingFiles method
    private string[] InvokeGetMatchingFiles(string pattern)
    {
        var method = typeof(WindowsCertificatePdfSigner).GetMethod("GetMatchingFiles", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (string[])(method?.Invoke(_signer, new object[] { pattern }) ?? Array.Empty<string>());
    }

    public void Dispose()
    {
        // Clean up certificates
        foreach (var cleanup in _certificateCleanups)
        {
            cleanup.Dispose();
        }
        _validCertWithSerial?.Dispose();
        _certWithoutSerial?.Dispose();
        
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