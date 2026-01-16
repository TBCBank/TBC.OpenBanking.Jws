// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests.Exceptions;

using TBC.OpenBanking.Jws.Exceptions;

/// <summary>
/// Unit tests for the <see cref="SignatureVerificationProblemException"/> class.
/// </summary>
public class SignatureVerificationProblemExceptionTests
{
    [Fact]
    public void Constructor_Default_CreatesException()
    {
        // Arrange & Act
        var exception = new SignatureVerificationProblemException();

        // Assert
        Assert.NotNull(exception);
        Assert.NotNull(exception.Message);
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Signature verification failed";

        // Act
        var exception = new SignatureVerificationProblemException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        var message = "Signature verification failed";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new SignatureVerificationProblemException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void HResult_IsSetCorrectly()
    {
        // Arrange & Act
        var exception = new SignatureVerificationProblemException();

        // Assert
        // ErrorCode 100 with SEVERITY_ERROR (1) and FACILITY_ITF (4)
        // HResult = (1 << 31) | (4 << 16) | 100 = 0x80040064
        Assert.Equal(unchecked((int)0x80040064), exception.HResult);
    }

    [Fact]
    public void IsJwsException_ReturnsTrue()
    {
        // Arrange & Act
        var exception = new SignatureVerificationProblemException();

        // Assert
        Assert.IsType<JwsException>(exception, exactMatch: false);
    }
}
