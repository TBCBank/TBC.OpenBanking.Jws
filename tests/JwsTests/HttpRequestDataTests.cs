// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests;

using TBC.OpenBanking.Jws.Exceptions;

/// <summary>
/// Unit tests for the <see cref="HttpRequestData"/> class.
/// Tests HTTP request data handling and header composition.
/// </summary>
public class HttpRequestDataTests
{
    [Fact]
    public void Constructor_Default_InitializesEmptyHeaders()
    {
        // Arrange & Act
        var requestData = new HttpRequestData();

        // Assert
        Assert.NotNull(requestData.Headers);
        Assert.Empty(requestData.Headers);
    }

    [Fact]
    public void Body_Default_IsEmptyArray()
    {
        // Arrange & Act
        var requestData = new HttpRequestData();

        // Assert
        Assert.NotNull(requestData.Body);
        Assert.Empty(requestData.Body);
    }

    [Fact]
    public void Uri_CanBeSetAndRetrieved()
    {
        // Arrange
        var requestData = new HttpRequestData();
        var uri = new Uri("https://example.com/api/resource?query=value");

        // Act
        requestData.Uri = uri;

        // Assert
        Assert.Equal(uri, requestData.Uri);
    }

    [Fact]
    public void Method_CanBeSetAndRetrieved()
    {
        // Arrange
        var requestData = new HttpRequestData();

        // Act
        requestData.Method = "POST";

        // Assert
        Assert.Equal("POST", requestData.Method);
    }

    [Fact]
    public void AddHeader_AddsHeaderToCollection()
    {
        // Arrange
        var requestData = new HttpRequestData();

        // Act
        requestData.AddHeader("Content-Type", "application/json");

        // Assert
        Assert.True(requestData.Headers.ContainsKey("Content-Type"));
        Assert.Equal("application/json", requestData.Headers["Content-Type"]);
    }

    [Fact]
    public void AddHeader_DuplicateKey_DoesNotOverwrite()
    {
        // Arrange
        var requestData = new HttpRequestData();
        requestData.AddHeader("Content-Type", "application/json");

        // Act
        requestData.AddHeader("Content-Type", "text/plain");

        // Assert
        Assert.Equal("application/json", requestData.Headers["Content-Type"]);
    }

    [Fact]
    public void AddHeader_NullName_ThrowsArgumentNullException()
    {
        // Arrange
        var requestData = new HttpRequestData();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => requestData.AddHeader(null!, "value"));
    }

    [Fact]
    public void AddHeader_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var requestData = new HttpRequestData();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => requestData.AddHeader("name", null!));
    }

    [Fact]
    public void Headers_CaseInsensitive()
    {
        // Arrange
        var requestData = new HttpRequestData();
        requestData.AddHeader("Content-Type", "application/json");

        // Act & Assert
        Assert.True(requestData.Headers.ContainsKey("content-type"));
        Assert.True(requestData.Headers.ContainsKey("CONTENT-TYPE"));
    }

    [Fact]
    public void ComposeHeadersForSignature_SingleHeader_ReturnsCorrectFormat()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "GET"
        };
        requestData.AddHeader("host", "example.com");

        var headers = new List<string> { "host" };

        // Act
        var result = requestData.ComposeHeadersForSignature(headers);

        // Assert
        Assert.Equal("host: example.com", result);
    }

    [Fact]
    public void ComposeHeadersForSignature_MultipleHeaders_SeparatedByNewline()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "GET"
        };
        requestData.AddHeader("host", "example.com");
        requestData.AddHeader("content-type", "application/json");

        var headers = new List<string> { "host", "content-type" };

        // Act
        var result = requestData.ComposeHeadersForSignature(headers);

        // Assert
        Assert.Equal("host: example.com\ncontent-type: application/json", result);
    }

    [Fact]
    public void ComposeHeadersForSignature_RequestTarget_ComposesCorrectly()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api/resource?query=value"),
            Method = "POST"
        };

        var headers = new List<string> { HttpRequestData.RequestTargetHeaderName };

        // Act
        var result = requestData.ComposeHeadersForSignature(headers);

        // Assert
        Assert.Equal("(request-target): post /api/resource?query=value", result);
    }

    [Fact]
    public void ComposeHeadersForSignature_RequestTargetAndHost_ComposesCorrectly()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "GET"
        };
        requestData.AddHeader("host", "example.com");

        var headers = new List<string> { HttpRequestData.RequestTargetHeaderName, "host" };

        // Act
        var result = requestData.ComposeHeadersForSignature(headers);

        // Assert
        Assert.Equal("(request-target): get /api\nhost: example.com", result);
    }

    [Fact]
    public void ComposeHeadersForSignature_AdditionalHeaders_UsesAdditionalHeaders()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "GET"
        };

        var headers = new List<string> { "digest" };
        var additionalHeaders = new Dictionary<string, string>
        {
            ["digest"] = "SHA-256=abc123"
        };

        // Act
        var result = requestData.ComposeHeadersForSignature(headers, additionalHeaders);

        // Assert
        Assert.Equal("digest: SHA-256=abc123", result);
    }

    [Fact]
    public void ComposeHeadersForSignature_MissingHeader_ThrowsHeaderMissingException()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "GET"
        };

        var headers = new List<string> { "non-existent-header" };

        // Act & Assert
        Assert.Throws<HeaderMissingException>(() => requestData.ComposeHeadersForSignature(headers));
    }

    [Fact]
    public void ComposeHeadersForSignature_NullHeaders_ThrowsArgumentNullException()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "GET"
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => requestData.ComposeHeadersForSignature(null!));
    }

    [Fact]
    public void CheckMandatoryHeaders_AllPresent_DoesNotThrow()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "GET"
        };
        requestData.AddHeader("host", "example.com");
        requestData.AddHeader("x-request-id", "123");
        requestData.AddHeader("digest", "SHA-256=abc");

        // Act & Assert - should not throw
        requestData.CheckMandatoryHeaders();
    }

    [Fact]
    public void CheckMandatoryHeaders_MissingHost_ThrowsHeaderMissingException()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "GET"
        };
        requestData.AddHeader("x-request-id", "123");
        requestData.AddHeader("digest", "SHA-256=abc");

        // Act & Assert
        var ex = Assert.Throws<HeaderMissingException>(() => requestData.CheckMandatoryHeaders());
        Assert.Contains("host", ex.Message);
    }

    [Fact]
    public void CheckMandatoryHeaders_MissingRequestId_ThrowsHeaderMissingException()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "GET"
        };
        requestData.AddHeader("host", "example.com");
        requestData.AddHeader("digest", "SHA-256=abc");

        // Act & Assert
        var ex = Assert.Throws<HeaderMissingException>(() => requestData.CheckMandatoryHeaders());
        Assert.Contains("x-request-id", ex.Message);
    }

    [Fact]
    public void CheckMandatoryHeaders_MissingDigest_ThrowsHeaderMissingException()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "GET"
        };
        requestData.AddHeader("host", "example.com");
        requestData.AddHeader("x-request-id", "123");

        // Act & Assert
        var ex = Assert.Throws<HeaderMissingException>(() => requestData.CheckMandatoryHeaders());
        Assert.Contains("digest", ex.Message);
    }

    [Fact]
    public void GetHeaderNamesForSignature_WithMandatoryHeaders_ReturnsCorrectList()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "GET"
        };
        requestData.AddHeader("host", "example.com");
        requestData.AddHeader("x-request-id", "123");
        requestData.AddHeader("digest", "SHA-256=abc");

        // Act
        var result = requestData.GetHeaderNamesForSignature();

        // Assert
        Assert.Contains("(request-target)", result);
        Assert.Contains("host", result);
        Assert.Contains("x-request-id", result);
        Assert.Contains("digest", result);
    }

    [Fact]
    public void GetHeaderNamesForSignature_WithContentHeaders_IncludesIfPresent()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "POST"
        };
        requestData.AddHeader("host", "example.com");
        requestData.AddHeader("x-request-id", "123");
        requestData.AddHeader("digest", "SHA-256=abc");
        requestData.AddHeader("content-type", "application/json");
        requestData.AddHeader("content-length", "100");

        // Act
        var result = requestData.GetHeaderNamesForSignature();

        // Assert
        Assert.Contains("content-type", result);
        Assert.Contains("content-length", result);
    }

    [Fact]
    public void GetHeaderNamesForSignature_WithPsuHeaders_IncludesPsuHeaders()
    {
        // Arrange
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "GET"
        };
        requestData.AddHeader("host", "example.com");
        requestData.AddHeader("x-request-id", "123");
        requestData.AddHeader("digest", "SHA-256=abc");
        requestData.AddHeader("psu-ip-address", "192.168.1.1");
        requestData.AddHeader("psu-user-agent", "Mozilla/5.0");

        // Act
        var result = requestData.GetHeaderNamesForSignature();

        // Assert
        Assert.Contains("psu-ip-address", result);
        Assert.Contains("psu-user-agent", result);
    }

    [Fact]
    public void Body_CanBeSetAndRetrieved()
    {
        // Arrange
        var requestData = new HttpRequestData();
        var body = "{ \"key\": \"value\" }"u8.ToArray();

        // Act
        requestData.Body = body;

        // Assert
        Assert.Equal(body, requestData.Body);
    }

    [Fact]
    public void NecessaryHeaders_ContainsExpectedHeaders()
    {
        // Assert
        Assert.Contains(HttpRequestData.NecessaryHeaders, h => h.Name == "(request-target)");
        Assert.Contains(HttpRequestData.NecessaryHeaders, h => h.Name == "host");
        Assert.Contains(HttpRequestData.NecessaryHeaders, h => h.Name == "x-request-id");
        Assert.Contains(HttpRequestData.NecessaryHeaders, h => h.Name == "digest");
    }
}
