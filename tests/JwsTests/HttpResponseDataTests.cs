// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests;

using TBC.OpenBanking.Jws.Exceptions;

/// <summary>
/// Unit tests for the <see cref="HttpResponseData"/> class.
/// Tests HTTP response data handling and header composition.
/// </summary>
public class HttpResponseDataTests
{
    [Fact]
    public void Constructor_Default_InitializesEmptyHeaders()
    {
        // Arrange & Act
        var responseData = new HttpResponseData();

        // Assert
        Assert.NotNull(responseData.Headers);
        Assert.Empty(responseData.Headers);
    }

    [Fact]
    public void Body_Default_IsEmptyArray()
    {
        // Arrange & Act
        var responseData = new HttpResponseData();

        // Assert
        Assert.NotNull(responseData.Body);
        Assert.Empty(responseData.Body);
    }

    [Fact]
    public void StatusCode_CanBeSetAndRetrieved()
    {
        // Arrange
        var responseData = new HttpResponseData();

        // Act
        responseData.StatusCode = "200";

        // Assert
        Assert.Equal("200", responseData.StatusCode);
    }

    [Fact]
    public void AddHeader_AddsHeaderToCollection()
    {
        // Arrange
        var responseData = new HttpResponseData();

        // Act
        responseData.AddHeader("Content-Type", "application/json");

        // Assert
        Assert.True(responseData.Headers.ContainsKey("Content-Type"));
        Assert.Equal("application/json", responseData.Headers["Content-Type"]);
    }

    [Fact]
    public void ComposeHeadersForSignature_ResponseStatus_ComposesCorrectly()
    {
        // Arrange
        var responseData = new HttpResponseData
        {
            StatusCode = "200"
        };

        var headers = new List<string> { HttpResponseData.ResponseStatusHeaderName };

        // Act
        var result = responseData.ComposeHeadersForSignature(headers);

        // Assert
        Assert.Equal("(response-status): 200", result);
    }

    [Fact]
    public void ComposeHeadersForSignature_MultipleHeaders_SeparatedByNewline()
    {
        // Arrange
        var responseData = new HttpResponseData
        {
            StatusCode = "200"
        };
        responseData.AddHeader("content-type", "application/json");
        responseData.AddHeader("x-request-id", "123");

        var headers = new List<string> { HttpResponseData.ResponseStatusHeaderName, "content-type", "x-request-id" };

        // Act
        var result = responseData.ComposeHeadersForSignature(headers);

        // Assert
        var expected = "(response-status): 200\ncontent-type: application/json\nx-request-id: 123";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ComposeHeadersForSignature_AdditionalHeaders_UsesAdditionalHeaders()
    {
        // Arrange
        var responseData = new HttpResponseData
        {
            StatusCode = "200"
        };

        var headers = new List<string> { "digest" };
        var additionalHeaders = new Dictionary<string, string>
        {
            ["digest"] = "SHA-256=abc123"
        };

        // Act
        var result = responseData.ComposeHeadersForSignature(headers, additionalHeaders);

        // Assert
        Assert.Equal("digest: SHA-256=abc123", result);
    }

    [Fact]
    public void ComposeHeadersForSignature_MissingHeader_ThrowsHeaderMissingException()
    {
        // Arrange
        var responseData = new HttpResponseData
        {
            StatusCode = "200"
        };

        var headers = new List<string> { "non-existent-header" };

        // Act & Assert
        Assert.Throws<HeaderMissingException>(() => responseData.ComposeHeadersForSignature(headers));
    }

    [Fact]
    public void ComposeHeadersForSignature_NullHeaders_ThrowsArgumentNullException()
    {
        // Arrange
        var responseData = new HttpResponseData
        {
            StatusCode = "200"
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => responseData.ComposeHeadersForSignature(null!));
    }

    [Fact]
    public void CheckMandatoryHeaders_AllPresent_DoesNotThrow()
    {
        // Arrange
        var responseData = new HttpResponseData
        {
            StatusCode = "200"
        };
        responseData.AddHeader("x-request-id", "123");
        responseData.AddHeader("digest", "SHA-256=abc");

        // Act & Assert - should not throw
        responseData.CheckMandatoryHeaders();
    }

    [Fact]
    public void CheckMandatoryHeaders_MissingRequestId_ThrowsHeaderMissingException()
    {
        // Arrange
        var responseData = new HttpResponseData
        {
            StatusCode = "200"
        };
        responseData.AddHeader("digest", "SHA-256=abc");

        // Act & Assert
        var ex = Assert.Throws<HeaderMissingException>(() => responseData.CheckMandatoryHeaders());
        Assert.Contains("x-request-id", ex.Message);
    }

    [Fact]
    public void CheckMandatoryHeaders_MissingDigest_ThrowsHeaderMissingException()
    {
        // Arrange
        var responseData = new HttpResponseData
        {
            StatusCode = "200"
        };
        responseData.AddHeader("x-request-id", "123");

        // Act & Assert
        var ex = Assert.Throws<HeaderMissingException>(() => responseData.CheckMandatoryHeaders());
        Assert.Contains("digest", ex.Message);
    }

    [Fact]
    public void GetHeaderNamesForSignature_WithMandatoryHeaders_ReturnsCorrectList()
    {
        // Arrange
        var responseData = new HttpResponseData
        {
            StatusCode = "200"
        };
        responseData.AddHeader("x-request-id", "123");
        responseData.AddHeader("digest", "SHA-256=abc");

        // Act
        var result = responseData.GetHeaderNamesForSignature();

        // Assert
        Assert.Contains("(response-status)", result);
        Assert.Contains("x-request-id", result);
        Assert.Contains("digest", result);
    }

    [Fact]
    public void GetHeaderNamesForSignature_WithContentHeaders_IncludesIfPresent()
    {
        // Arrange
        var responseData = new HttpResponseData
        {
            StatusCode = "200"
        };
        responseData.AddHeader("x-request-id", "123");
        responseData.AddHeader("digest", "SHA-256=abc");
        responseData.AddHeader("content-type", "application/json");
        responseData.AddHeader("content-length", "100");

        // Act
        var result = responseData.GetHeaderNamesForSignature();

        // Assert
        Assert.Contains("content-type", result);
        Assert.Contains("content-length", result);
    }

    [Fact]
    public void Body_CanBeSetAndRetrieved()
    {
        // Arrange
        var responseData = new HttpResponseData();
        var body = "{ \"result\": \"success\" }"u8.ToArray();

        // Act
        responseData.Body = body;

        // Assert
        Assert.Equal(body, responseData.Body);
    }

    [Fact]
    public void NecessaryHeaders_ContainsExpectedHeaders()
    {
        // Assert
        Assert.Contains(HttpResponseData.NecessaryHeaders, h => h.Name == "(response-status)");
        Assert.Contains(HttpResponseData.NecessaryHeaders, h => h.Name == "x-request-id");
        Assert.Contains(HttpResponseData.NecessaryHeaders, h => h.Name == "digest");
    }

    [Fact]
    public void ResponseStatusHeaderName_HasExpectedValue()
    {
        // Assert
        Assert.Equal("(response-status)", HttpResponseData.ResponseStatusHeaderName);
    }

    [Theory]
    [InlineData("200")]
    [InlineData("201")]
    [InlineData("400")]
    [InlineData("404")]
    [InlineData("500")]
    public void ComposeHeadersForSignature_VariousStatusCodes_ComposesCorrectly(string statusCode)
    {
        // Arrange
        var responseData = new HttpResponseData
        {
            StatusCode = statusCode
        };

        var headers = new List<string> { HttpResponseData.ResponseStatusHeaderName };

        // Act
        var result = responseData.ComposeHeadersForSignature(headers);

        // Assert
        Assert.Equal($"(response-status): {statusCode}", result);
    }
}
