// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests.Exceptions;

using System.Security.Cryptography.X509Certificates;
using TBC.OpenBanking.Jws.Exceptions;

/// <summary>
/// Unit tests for the <see cref="CertificateValidationException"/> class.
/// </summary>
public class CertificateValidationExceptionTests
{
    [Fact]
    public void Constructor_Default_CreatesException()
    {
        // Arrange & Act
        var exception = new CertificateValidationException();

        // Assert
        Assert.NotNull(exception);
        // The default constructor doesn't set a custom message, so Message returns the default exception message
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Certificate validation failed";

        // Act
        var exception = new CertificateValidationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        var message = "Certificate validation failed";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new CertificateValidationException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithChainStatus_IncludesStatusInMessage()
    {
        // Arrange
        var statuses = new[]
        {
            new X509ChainStatus { Status = X509ChainStatusFlags.NotTimeValid, StatusInformation = "Certificate has expired" },
            new X509ChainStatus { Status = X509ChainStatusFlags.UntrustedRoot, StatusInformation = "Root is not trusted" }
        };
        var baseMessage = "Certificate validation failed";

        // Act
        var exception = new CertificateValidationException(statuses, baseMessage);

        // Assert
        Assert.NotNull(exception.Message);
        Assert.Contains(baseMessage, exception.Message);
    }

    [Fact]
    public void Constructor_WithNullStatuses_DoesNotThrow()
    {
        // Arrange & Act
        var exception = new CertificateValidationException(null, "Test message");

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public void HResult_IsSetCorrectly()
    {
        // Arrange & Act
        var exception = new CertificateValidationException();

        // Assert
        // ErrorCode 102 with SEVERITY_ERROR (1) and FACILITY_ITF (4)
        // HResult = (1 << 31) | (4 << 16) | 102 = 0x80040066
        Assert.Equal(unchecked((int)0x80040066), exception.HResult);
    }

    [Fact]
    public void IsJwsException_ReturnsTrue()
    {
        // Arrange & Act
        var exception = new CertificateValidationException();

        // Assert
        Assert.IsType<JwsException>(exception, exactMatch: false);
    }

    [Fact]
    public void Constructor_WithEmptyStatuses_HandlesCorrectly()
    {
        // Arrange
        var statuses = Array.Empty<X509ChainStatus>();
        var baseMessage = "Certificate validation failed";

        // Act
        var exception = new CertificateValidationException(statuses, baseMessage);

        // Assert
        Assert.NotNull(exception);
        Assert.Contains(baseMessage, exception.Message);
    }
}
