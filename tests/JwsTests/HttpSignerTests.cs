// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests;

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using TBC.OpenBanking.Jws.Tests.Helpers;

/// <summary>
/// Unit tests for the <see cref="HttpSigner{T}"/> class.
/// Tests HTTP request/response signing functionality.
/// </summary>
public class HttpSignerTests
{
    [Fact]
    public void Constructor_WithLogger_CreatesInstance()
    {
        // Arrange & Act
        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance);

        // Assert
        Assert.NotNull(signer);
        Assert.False(signer.IsSignatureCreated);
        Assert.Null(signer.SignatureHeaderValue);
        Assert.Null(signer.DigestHeaderValue);
    }

    [Fact]
    public void Constructor_WithNullLogger_CreatesInstanceWithNullLogger()
    {
        // Arrange & Act
        var signer = new HttpSigner<HttpRequestData>(null!);

        // Assert
        Assert.NotNull(signer);
    }

    [Fact]
    public void CreateSignature_WithValidData_ReturnsTrue()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var algorithm = new AlgorithmRsaSsa(
            cert,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance)
        {
            Signer = algorithm,
            SignerCertificate = cert,
            SignerCertificateChain = []
        };

        var requestData = CreateValidHttpRequestData();

        // Act
        var result = signer.CreateSignature(requestData);

        // Assert
        Assert.True(result);
        Assert.True(signer.IsSignatureCreated);
        Assert.NotNull(signer.SignatureHeaderValue);
        Assert.NotNull(signer.DigestHeaderValue);
        Assert.NotNull(signer.ProtectedHeader);
    }

    [Fact]
    public void CreateSignature_WithNullHttpData_ThrowsArgumentNullException()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var algorithm = new AlgorithmRsaSsa(
            cert,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance)
        {
            Signer = algorithm,
            SignerCertificate = cert,
            SignerCertificateChain = []
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => signer.CreateSignature(null!));
    }

    [Fact]
    public void CreateSignature_WithoutSigner_ThrowsInvalidOperationException()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();

        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance)
        {
            SignerCertificate = cert,
            SignerCertificateChain = []
        };

        var requestData = CreateValidHttpRequestData();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => signer.CreateSignature(requestData));
    }

    [Fact]
    public void CreateSignature_WithoutSignerCertificate_ThrowsInvalidOperationException()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var algorithm = new AlgorithmRsaSsa(
            cert,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance)
        {
            Signer = algorithm,
            SignerCertificateChain = []
        };

        var requestData = CreateValidHttpRequestData();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => signer.CreateSignature(requestData));
    }

    [Fact]
    public void CreateSignature_WithoutSignerCertificateChain_ThrowsInvalidOperationException()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var algorithm = new AlgorithmRsaSsa(
            cert,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance)
        {
            Signer = algorithm,
            SignerCertificate = cert
        };

        var requestData = CreateValidHttpRequestData();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => signer.CreateSignature(requestData));
    }

    [Fact]
    public void SignatureHeaderValue_ContainsTwoParts()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var algorithm = new AlgorithmRsaSsa(
            cert,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance)
        {
            Signer = algorithm,
            SignerCertificate = cert,
            SignerCertificateChain = []
        };

        var requestData = CreateValidHttpRequestData();

        // Act
        signer.CreateSignature(requestData);

        // Assert
        Assert.NotNull(signer.SignatureHeaderValue);
        Assert.Contains("..", signer.SignatureHeaderValue);

        var parts = signer.SignatureHeaderValue.Split("..");
        Assert.Equal(2, parts.Length);
    }

    [Fact]
    public void DigestHeaderValue_HasCorrectFormat()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var algorithm = new AlgorithmRsaSsa(
            cert,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance)
        {
            Signer = algorithm,
            SignerCertificate = cert,
            SignerCertificateChain = []
        };

        var requestData = CreateValidHttpRequestData();

        // Act
        signer.CreateSignature(requestData);

        // Assert
        Assert.NotNull(signer.DigestHeaderValue);
        Assert.StartsWith("SHA-256=", signer.DigestHeaderValue);
    }

    [Fact]
    public void CreateSignature_CalledTwice_ResetsState()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var algorithm = new AlgorithmRsaSsa(
            cert,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance)
        {
            Signer = algorithm,
            SignerCertificate = cert,
            SignerCertificateChain = []
        };

        var requestData1 = CreateValidHttpRequestData();
        requestData1.Body = "First body"u8.ToArray();

        var requestData2 = CreateValidHttpRequestData();
        requestData2.Body = "Second body"u8.ToArray();

        // Act
        signer.CreateSignature(requestData1);
        var firstDigest = signer.DigestHeaderValue;

        signer.CreateSignature(requestData2);
        var secondDigest = signer.DigestHeaderValue;

        // Assert
        Assert.NotEqual(firstDigest, secondDigest);
    }

    [Fact]
    public void DigestHashAlgorithmName_CanBeSet()
    {
        // Arrange
        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance);

        // Act
        signer.DigestHashAlgorithmName = System.Security.Cryptography.HashAlgorithmName.SHA512;

        // Assert
        Assert.Equal(System.Security.Cryptography.HashAlgorithmName.SHA512, signer.DigestHashAlgorithmName);
    }

    [Fact]
    public void CreateSignature_WithCustomDigestAlgorithm_UsesCustomAlgorithm()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var algorithm = new AlgorithmRsaSsa(
            cert,
            System.Security.Cryptography.HashAlgorithmName.SHA512,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance)
        {
            Signer = algorithm,
            SignerCertificate = cert,
            SignerCertificateChain = [],
            DigestHashAlgorithmName = System.Security.Cryptography.HashAlgorithmName.SHA512
        };

        var requestData = CreateValidHttpRequestData();

        // Act
        signer.CreateSignature(requestData);

        // Assert
        Assert.StartsWith("SHA-512=", signer.DigestHeaderValue);
    }

    [Fact]
    public void ProtectedHeader_ContainsCorrectData()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var algorithm = new AlgorithmRsaSsa(
            cert,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance)
        {
            Signer = algorithm,
            SignerCertificate = cert,
            SignerCertificateChain = []
        };

        var requestData = CreateValidHttpRequestData();

        // Act
        signer.CreateSignature(requestData);

        // Assert
        Assert.NotNull(signer.ProtectedHeader);
        Assert.Equal("RS256", signer.ProtectedHeader.AlgorithmName);
        Assert.NotEmpty(signer.ProtectedHeader.EncodedCertificates);
        Assert.NotEmpty(signer.ProtectedHeader.DataToBeSigned.Parameters);
    }

    [Fact]
    public void CreateSignature_ForHttpResponseData_ReturnsTrue()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var algorithm = new AlgorithmRsaSsa(
            cert,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var signer = new HttpSigner<HttpResponseData>(NullLogger<HttpSigner<HttpResponseData>>.Instance)
        {
            Signer = algorithm,
            SignerCertificate = cert,
            SignerCertificateChain = []
        };

        var responseData = new HttpResponseData
        {
            StatusCode = "200",
            Body = "{ \"result\": \"success\" }"u8.ToArray()
        };
        responseData.AddHeader("x-request-id", "123");
        responseData.AddHeader("digest", "SHA-256=placeholder");

        // Act
        var result = signer.CreateSignature(responseData);

        // Assert
        Assert.True(result);
        Assert.True(signer.IsSignatureCreated);
    }

    [Fact]
    public void CreateSignature_WithCertificateChain_IncludesChainInProtectedHeader()
    {
        // Arrange
        var (endCert, chain) = CertificateHelper.CreateCertificateChain(isRsa: true);
        using var algorithm = new AlgorithmRsaSsa(
            endCert,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance)
        {
            Signer = algorithm,
            SignerCertificate = endCert,
            SignerCertificateChain = chain
        };

        var requestData = CreateValidHttpRequestData();

        // Act
        signer.CreateSignature(requestData);

        // Assert
        Assert.NotNull(signer.ProtectedHeader);
        Assert.Equal(2, signer.ProtectedHeader.EncodedCertificates.Count);

        // Cleanup
        endCert.Dispose();
        foreach (var c in chain)
        {
            c.Dispose();
        }
    }

    private static HttpRequestData CreateValidHttpRequestData()
    {
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api/resource"),
            Method = "POST",
            Body = "{ \"key\": \"value\" }"u8.ToArray()
        };
        requestData.AddHeader("host", "example.com");
        requestData.AddHeader("x-request-id", Guid.NewGuid().ToString());
        requestData.AddHeader("content-type", "application/json");
        requestData.AddHeader("content-length", requestData.Body.Length.ToString());
        requestData.AddHeader("digest", "SHA-256=placeholder"); // Will be overwritten

        return requestData;
    }
}
