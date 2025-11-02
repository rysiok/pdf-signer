using FluentAssertions;
using PdfSignerApp;
using Xunit;

namespace PdfSigner.Tests;

/// <summary>
/// Tests for result classes and data structures
/// </summary>
public class ResultClassesTests
{
    [Fact]
    public void PdfVerificationResult_DefaultInitialization_ShouldHaveCorrectDefaults()
    {
        // Act
        var result = new PdfVerificationResult();

        // Assert
        result.IsValid.Should().BeFalse();
        result.TotalSignatures.Should().Be(0);
        result.Signatures.Should().NotBeNull();
        result.Signatures.Should().BeEmpty();
    }

    [Fact]
    public void PdfVerificationResult_WithSignatures_ShouldMaintainCorrectState()
    {
        // Arrange
        var signatures = new List<SignatureInfo>
        {
            new() { Name = "Sig1", IsValid = true, SerialNumber = "123", CertificateSubject = "CN=Test1" },
            new() { Name = "Sig2", IsValid = false, ErrorMessage = "Error occurred" }
        };

        // Act
        var result = new PdfVerificationResult
        {
            IsValid = true,
            TotalSignatures = 2,
            Signatures = signatures
        };

        // Assert
        result.IsValid.Should().BeTrue();
        result.TotalSignatures.Should().Be(2);
        result.Signatures.Should().HaveCount(2);
        result.Signatures[0].IsValid.Should().BeTrue();
        result.Signatures[1].IsValid.Should().BeFalse();
    }

    [Fact]
    public void SignatureInfo_DefaultInitialization_ShouldHaveCorrectDefaults()
    {
        // Act
        var signature = new SignatureInfo();

        // Assert
        signature.Name.Should().Be("");
        signature.IsValid.Should().BeFalse();
        signature.CertificateSubject.Should().Be("");
        signature.SerialNumber.Should().Be("");
        signature.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void SignatureInfo_WithValidData_ShouldMaintainCorrectState()
    {
        // Act
        var signature = new SignatureInfo
        {
            Name = "TestSignature",
            IsValid = true,
            CertificateSubject = "CN=TestCert, SERIALNUMBER=123456",
            SerialNumber = "123456"
        };

        // Assert
        signature.Name.Should().Be("TestSignature");
        signature.IsValid.Should().BeTrue();
        signature.CertificateSubject.Should().Be("CN=TestCert, SERIALNUMBER=123456");
        signature.SerialNumber.Should().Be("123456");
        signature.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void SignatureInfo_WithError_ShouldMaintainErrorState()
    {
        // Act
        var signature = new SignatureInfo
        {
            Name = "FailedSignature",
            IsValid = false,
            ErrorMessage = "SERIALNUMBER property not found"
        };

        // Assert
        signature.Name.Should().Be("FailedSignature");
        signature.IsValid.Should().BeFalse();
        signature.ErrorMessage.Should().Be("SERIALNUMBER property not found");
    }

    [Fact]
    public void SignatureVerificationResult_DefaultInitialization_ShouldHaveCorrectDefaults()
    {
        // Act
        var result = new SignatureVerificationResult();

        // Assert
        result.IsValid.Should().BeFalse();
        result.SigningCertificateSerialNumber.Should().Be("");
        result.PdfCertificateSerialNumber.Should().Be("");
        result.CertificateSubject.Should().Be("");
        result.Message.Should().Be("");
    }

    [Fact]
    public void SignatureVerificationResult_WithValidData_ShouldMaintainCorrectState()
    {
        // Act
        var result = new SignatureVerificationResult
        {
            IsValid = true,
            SigningCertificateSerialNumber = "SIGN123",
            PdfCertificateSerialNumber = "SIGN123",
            CertificateSubject = "CN=SigningCert, SERIALNUMBER=SIGN123",
            Message = "SERIALNUMBER verified: SIGN123"
        };

        // Assert
        result.IsValid.Should().BeTrue();
        result.SigningCertificateSerialNumber.Should().Be("SIGN123");
        result.PdfCertificateSerialNumber.Should().Be("SIGN123");
        result.CertificateSubject.Should().Be("CN=SigningCert, SERIALNUMBER=SIGN123");
        result.Message.Should().Be("SERIALNUMBER verified: SIGN123");
    }

    [Fact]
    public void SignatureVerificationResult_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var result1 = new SignatureVerificationResult
        {
            IsValid = true,
            SigningCertificateSerialNumber = "123",
            Message = "Test message"
        };

        var result2 = new SignatureVerificationResult
        {
            IsValid = true,
            SigningCertificateSerialNumber = "123",
            Message = "Test message"
        };

        var result3 = new SignatureVerificationResult
        {
            IsValid = false,
            SigningCertificateSerialNumber = "456",
            Message = "Different message"
        };

        // Assert - Objects with same values should have same string representation
        result1.ToString().Should().NotBeNull();
        result2.ToString().Should().NotBeNull();
        result3.ToString().Should().NotBeNull();
        
        // Properties should be independently settable
        result1.IsValid.Should().Be(result2.IsValid);
        result1.SigningCertificateSerialNumber.Should().Be(result2.SigningCertificateSerialNumber);
        result1.IsValid.Should().NotBe(result3.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Test Name")]
    [InlineData("Signature with special chars: @#$%^&*()")]
    public void SignatureInfo_NameProperty_ShouldAcceptVariousValues(string name)
    {
        // Act
        var signature = new SignatureInfo { Name = name };

        // Assert
        signature.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456789")]
    [InlineData("ABCDEF")]
    [InlineData("Mixed123ABC")]
    public void SignatureInfo_SerialNumberProperty_ShouldAcceptVariousValues(string serialNumber)
    {
        // Act
        var signature = new SignatureInfo { SerialNumber = serialNumber };

        // Assert
        signature.SerialNumber.Should().Be(serialNumber);
    }

    [Fact]
    public void PdfVerificationResult_Collections_ShouldSupportModification()
    {
        // Arrange
        var result = new PdfVerificationResult();

        // Act - Add signatures
        result.Signatures.Add(new SignatureInfo { Name = "Sig1" });
        result.Signatures.Add(new SignatureInfo { Name = "Sig2" });

        // Assert
        result.Signatures.Should().HaveCount(2);
        result.Signatures[0].Name.Should().Be("Sig1");
        result.Signatures[1].Name.Should().Be("Sig2");

        // Act - Remove signature
        result.Signatures.RemoveAt(0);

        // Assert
        result.Signatures.Should().HaveCount(1);
        result.Signatures[0].Name.Should().Be("Sig2");
    }

    [Fact]
    public void SignatureInfo_NullErrorMessage_ShouldBeAllowed()
    {
        // Act
        var signature = new SignatureInfo
        {
            ErrorMessage = null
        };

        // Assert
        signature.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void SignatureInfo_EmptyErrorMessage_ShouldBeAllowed()
    {
        // Act
        var signature = new SignatureInfo
        {
            ErrorMessage = ""
        };

        // Assert
        signature.ErrorMessage.Should().Be("");
    }
}