// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using TBC.OpenBanking.Jws.Tests.Helpers;

/// <summary>
/// Unit tests for the <see cref="AlgorithmRsaSsa"/> class.
/// Tests RSA signature algorithms (RS256, RS384, RS512, PS256, PS384, PS512).
/// </summary>
public class AlgorithmRsaSsaTests
{
    [Theory]
    [InlineData(2048)]
    [InlineData(3072)]
    [InlineData(4096)]
    public void Constructor_WithKeySize_CreatesValidAlgorithm(int keySize)
    {
        // Arrange & Act
        using var algorithm = new AlgorithmRsaSsa(keySize, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Assert
        Assert.NotNull(algorithm);
        Assert.Equal("RS256", algorithm.Name);
        Assert.Equal(HashAlgorithmName.SHA256, algorithm.HashAlgorithmName);
    }

    [Theory]
    [MemberData(nameof(GetPkcs1AlgorithmCombinations), DisableDiscoveryEnumeration = true)]
    public void Constructor_WithPkcs1Padding_ReturnsCorrectAlgorithmName(string expectedName, HashAlgorithmName hashAlgorithm)
    {
        // Arrange & Act
        using var algorithm = new AlgorithmRsaSsa(2048, hashAlgorithm, RSASignaturePadding.Pkcs1);

        // Assert
        Assert.Equal(expectedName, algorithm.Name);
    }

    [Theory]
    [MemberData(nameof(GetPssAlgorithmCombinations), DisableDiscoveryEnumeration = true)]
    public void Constructor_WithPssPadding_ReturnsCorrectAlgorithmName(string expectedName, HashAlgorithmName hashAlgorithm)
    {
        // Arrange & Act
        using var algorithm = new AlgorithmRsaSsa(2048, hashAlgorithm, RSASignaturePadding.Pss);

        // Assert
        Assert.Equal(expectedName, algorithm.Name);
    }

    [Fact]
    public void Constructor_WithCertificate_CreatesValidAlgorithm()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();

        // Act
        using var algorithm = new AlgorithmRsaSsa(cert, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Assert
        Assert.NotNull(algorithm);
        Assert.Equal("RS256", algorithm.Name);
    }

    [Fact]
    public void Constructor_WithNullCertificate_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        X509Certificate2? nullCert = null;
        Assert.Throws<ArgumentNullException>(() =>
            new AlgorithmRsaSsa(nullCert!, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
    }

    [Fact]
    public void Constructor_WithNullPadding_ThrowsArgumentNullException()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AlgorithmRsaSsa(cert, HashAlgorithmName.SHA256, null!));
    }

    [Fact]
    public void Constructor_WithRsaInstance_CreatesValidAlgorithm()
    {
        // Arrange
        using var rsa = RSA.Create(2048);

        // Act
        using var algorithm = new AlgorithmRsaSsa(rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Assert
        Assert.NotNull(algorithm);
        Assert.Equal("RS256", algorithm.Name);
    }

    [Fact]
    public void Constructor_WithRsaParameters_CreatesValidAlgorithm()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var parameters = rsa.ExportParameters(true);

        // Act
        using var algorithm = new AlgorithmRsaSsa(parameters, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Assert
        Assert.NotNull(algorithm);
        Assert.Equal("RS256", algorithm.Name);
    }

    [Fact]
    public void Sign_ValidData_ReturnsBase64UrlEncodedSignature()
    {
        // Arrange
        using var algorithm = new AlgorithmRsaSsa(2048, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var header = "eyJhbGciOiJSUzI1NiJ9";
        var payload = "eyJpc3MiOiJqb2UifQ";

        // Act
        var signature = algorithm.Sign(header, payload);

        // Assert
        Assert.NotNull(signature);
        Assert.NotEmpty(signature);
        // Base64Url should not contain '+', '/', or '='
        Assert.DoesNotContain("+", signature);
        Assert.DoesNotContain("/", signature);
    }

    [Fact]
    public void SignData_ValidData_ReturnsSignatureBytes()
    {
        // Arrange
        using var algorithm = new AlgorithmRsaSsa(2048, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var data = "test data"u8.ToArray();

        // Act
        var signature = algorithm.SignData(data);

        // Assert
        Assert.NotNull(signature);
        Assert.NotEmpty(signature);
    }

    [Fact]
    public void VerifySignature_ValidSignature_ReturnsTrue()
    {
        // Arrange
        using var algorithm = new AlgorithmRsaSsa(2048, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var header = "eyJhbGciOiJSUzI1NiJ9";
        var payload = "eyJpc3MiOiJqb2UifQ";
        var signature = algorithm.Sign(header, payload);

        // Act
        var result = algorithm.VerifySignature(header, payload, signature);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifySignature_InvalidSignature_ReturnsFalse()
    {
        // Arrange
        using var algorithm = new AlgorithmRsaSsa(2048, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var header = "eyJhbGciOiJSUzI1NiJ9";
        var payload = "eyJpc3MiOiJqb2UifQ";
        var signature = algorithm.Sign(header, payload);
        var tamperedSignature = "invalid" + signature[7..];

        // Act
        var result = algorithm.VerifySignature(header, payload, tamperedSignature);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifySignature_TamperedPayload_ReturnsFalse()
    {
        // Arrange
        using var algorithm = new AlgorithmRsaSsa(2048, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var header = "eyJhbGciOiJSUzI1NiJ9";
        var payload = "eyJpc3MiOiJqb2UifQ";
        var signature = algorithm.Sign(header, payload);
        var tamperedPayload = "eyJpc3MiOiJqYW5lIn0"; // Different issuer

        // Act
        var result = algorithm.VerifySignature(header, tamperedPayload, signature);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(GetAllRsaAlgorithmCombinations), DisableDiscoveryEnumeration = true)]
    public void SignAndVerify_AllAlgorithmCombinations_WorksCorrectly(HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
    {
        // Arrange
        using var algorithm = new AlgorithmRsaSsa(2048, hashAlgorithm, padding);
        var header = "eyJhbGciOiJSUzI1NiJ9";
        var payload = "eyJpc3MiOiJqb2UifQ";

        // Act
        var signature = algorithm.Sign(header, payload);
        var isValid = algorithm.VerifySignature(header, payload, signature);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var algorithm = new AlgorithmRsaSsa(2048, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Act & Assert - should not throw
        algorithm.Dispose();
        algorithm.Dispose();
    }

    [Fact]
    public void GetJwk_NotImplemented_ThrowsNotImplementedException()
    {
        // Arrange
        using var algorithm = new AlgorithmRsaSsa(2048, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Act & Assert
        Assert.Throws<NotImplementedException>(() => algorithm.GetJwk(false));
    }

    public static TheoryData<HashAlgorithmName, RSASignaturePadding> GetAllRsaAlgorithmCombinations()
    {
        var data = new TheoryData<HashAlgorithmName, RSASignaturePadding>
        {
            { HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1 },
            { HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1 },
            { HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1 },
            { HashAlgorithmName.SHA256, RSASignaturePadding.Pss },
            { HashAlgorithmName.SHA384, RSASignaturePadding.Pss },
            { HashAlgorithmName.SHA512, RSASignaturePadding.Pss },
        };

        return data;
    }

    public static TheoryData<string, HashAlgorithmName> GetPkcs1AlgorithmCombinations()
    {
        var data = new TheoryData<string, HashAlgorithmName>
        {
            { "RS256", HashAlgorithmName.SHA256 },
            { "RS386", HashAlgorithmName.SHA384 },
            { "RS512", HashAlgorithmName.SHA512 },
        };

        return data;
    }

    public static TheoryData<string, HashAlgorithmName> GetPssAlgorithmCombinations()
    {
        var data = new TheoryData<string, HashAlgorithmName>
        {
            { "PS256", HashAlgorithmName.SHA256 },
            { "PS386", HashAlgorithmName.SHA384 },
            { "PS512", HashAlgorithmName.SHA512 },
        };

        return data;
    }
}
