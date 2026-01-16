// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests.Exceptions;

using TBC.OpenBanking.Jws.Exceptions;

/// <summary>
/// Unit tests for the <see cref="HeaderMissingException"/> class.
/// </summary>
public class HeaderMissingExceptionTests
{
    [Fact]
    public void Constructor_Default_CreatesException()
    {
        // Arrange & Act
        var exception = new HeaderMissingException();

        // Assert
        Assert.NotNull(exception);
        Assert.NotNull(exception.Message);
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Header 'Content-Type' is missing";

        // Act
        var exception = new HeaderMissingException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        var message = "Header is missing";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new HeaderMissingException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void HResult_IsSetCorrectly()
    {
        // Arrange & Act
        var exception = new HeaderMissingException();

        // Assert
        // ErrorCode 101 with SEVERITY_ERROR (1) and FACILITY_ITF (4)
        // HResult = (1 << 31) | (4 << 16) | 101 = 0x80040065
        Assert.Equal(unchecked((int)0x80040065), exception.HResult);
    }

    [Fact]
    public void IsJwsException_ReturnsTrue()
    {
        // Arrange & Act
        var exception = new HeaderMissingException();

        // Assert
        Assert.IsType<JwsException>(exception, exactMatch: false);
    }
}
