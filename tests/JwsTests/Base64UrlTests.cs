// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests;

/// <summary>
/// Unit tests for the <see cref="Base64Url"/> class.
/// Tests the base64url encoding/decoding functionality as per RFC 4648 Section 5.
/// </summary>
public class Base64UrlTests
{
    [Fact]
    public void EncodeBase64Url_EmptyArray_ReturnsEmptyString()
    {
        // Arrange
        var data = Array.Empty<byte>();

        // Act
        var result = data.EncodeBase64Url();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void DecodeBase64Url_EmptyString_ReturnsEmptyArray()
    {
        // Arrange
        var base64Url = string.Empty;

        // Act
        var result = base64Url.DecodeBase64Url();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void EncodeBase64Url_SimpleData_ReturnsCorrectEncoding()
    {
        // Arrange
        // "Hello" in bytes
        var data = "Hello"u8.ToArray();

        // Act
        var result = data.EncodeBase64Url();

        // Assert
        // Standard Base64 for "Hello" is "SGVsbG8=", Base64Url removes padding
        Assert.Equal("SGVsbG8", result);
    }

    [Fact]
    public void DecodeBase64Url_SimpleData_ReturnsCorrectBytes()
    {
        // Arrange
        var base64Url = "SGVsbG8";

        // Act
        var result = base64Url.DecodeBase64Url();

        // Assert
        Assert.Equal("Hello"u8.ToArray(), result);
    }

    [Fact]
    public void EncodeBase64Url_DataWithSpecialCharacters_UsesUrlSafeCharacters()
    {
        // Arrange
        // Data that produces '+' and '/' in standard Base64
        var data = new byte[] { 0xfb, 0xff, 0xbf }; // Produces "+/+/" in standard Base64

        // Act
        var result = data.EncodeBase64Url();

        // Assert
        // Base64Url should use '-' and '_' instead of '+' and '/'
        Assert.DoesNotContain("+", result);
        Assert.DoesNotContain("/", result);
    }

    [Fact]
    public void RoundTrip_VariousData_DecodedDataEqualsOriginal()
    {
        // Arrange
        var testCases = new[]
        {
            Array.Empty<byte>(),
            new byte[] { 0x00 },
            new byte[] { 0x00, 0x01, 0x02 },
            "Test data with various characters!"u8.ToArray(),
            Enumerable.Range(0, 256).Select(i => (byte)i).ToArray(), // All byte values
        };

        foreach (var original in testCases)
        {
            // Act
            var encoded = original.EncodeBase64Url();
            var decoded = encoded.DecodeBase64Url();

            // Assert
            Assert.Equal(original, decoded);
        }
    }

    [Fact]
    public void EncodeBase64Url_NoPaddingCharacters_ReturnsStringWithoutEquals()
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        // Act
        var result = data.EncodeBase64Url();

        // Assert
        Assert.DoesNotContain("=", result);
    }

    [Fact]
    public void DecodeBase64Url_WithPadding_HandlesCorrectly()
    {
        // Arrange
        // "Hello" encoded with padding
        var base64UrlWithPadding = "SGVsbG8=";

        // Act
        var result = base64UrlWithPadding.DecodeBase64Url();

        // Assert
        Assert.Equal("Hello"u8.ToArray(), result);
    }

    [Fact]
    public void DecodeBase64Url_WithoutPadding_HandlesCorrectly()
    {
        // Arrange
        // "Hello" encoded without padding
        var base64UrlWithoutPadding = "SGVsbG8";

        // Act
        var result = base64UrlWithoutPadding.DecodeBase64Url();

        // Assert
        Assert.Equal("Hello"u8.ToArray(), result);
    }

    [Fact]
    public void EncodeBase64Url_LargeData_EncodesCorrectly()
    {
        // Arrange
        var data = new byte[10000];
        Random.Shared.NextBytes(data);

        // Act
        var encoded = data.EncodeBase64Url();
        var decoded = encoded.DecodeBase64Url();

        // Assert
        Assert.Equal(data, decoded);
    }
}
