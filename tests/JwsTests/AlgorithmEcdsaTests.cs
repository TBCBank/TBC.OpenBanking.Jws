// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests;

using System.Security.Cryptography;
using TBC.OpenBanking.Jws.Tests.Helpers;

/// <summary>
/// Unit tests for the <see cref="AlgorithmEcdsa"/> class.
/// Tests ECDSA signature algorithms (ES256, ES384, ES512).
/// </summary>
public class AlgorithmEcdsaTests
{
    [Theory]
    [InlineData(256)]
    [InlineData(384)]
    [InlineData(521)] // P-521, not 512
    public void Constructor_WithKeySize_CreatesValidAlgorithm(int keySize)
    {
        // Arrange & Act
        using var algorithm = new AlgorithmEcdsa(keySize, GetHashAlgorithmForKeySize(keySize));

        // Assert
        Assert.NotNull(algorithm);
    }

    [Theory]
    [InlineData("ES256", 256)]
    [InlineData("ES384", 384)]
    [InlineData("ES512", 521)]
    public void Constructor_WithKeySize_ReturnsCorrectAlgorithmName(string expectedName, int keySize)
    {
        // Arrange & Act
        using var algorithm = new AlgorithmEcdsa(keySize, GetHashAlgorithmForKeySize(keySize));

        // Assert
        Assert.Equal(expectedName, algorithm.Name);
    }

    [Fact]
    public void Constructor_WithCertificate_CreatesValidAlgorithm()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedEcdsaCertificate();

        // Act
        using var algorithm = new AlgorithmEcdsa(cert, HashAlgorithmName.SHA256);

        // Assert
        Assert.NotNull(algorithm);
        Assert.Equal("ES256", algorithm.Name);
        Assert.Equal(HashAlgorithmName.SHA256, algorithm.HashAlgorithmName);
    }

    [Fact]
    public void Constructor_WithNullCertificate_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AlgorithmEcdsa((System.Security.Cryptography.X509Certificates.X509Certificate2)null!, HashAlgorithmName.SHA256));
    }

    [Fact]
    public void Constructor_WithCertificateWithoutPrivateKey_ThrowsArgumentException()
    {
        // Arrange
        using var certWithPrivateKey = CertificateHelper.CreateSelfSignedEcdsaCertificate();
        using var certWithoutPrivateKey = CertificateHelper.GetPublicKeyOnlyCertificate(certWithPrivateKey);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new AlgorithmEcdsa(certWithoutPrivateKey, HashAlgorithmName.SHA256));
    }

    [Fact]
    public void Constructor_WithECDsaInstance_CreatesValidAlgorithm()
    {
        // Arrange
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Act
        using var algorithm = new AlgorithmEcdsa(ecdsa, HashAlgorithmName.SHA256);

        // Assert
        Assert.NotNull(algorithm);
        Assert.Equal("ES256", algorithm.Name);
    }

    [Fact]
    public void Constructor_WithECParameters_CreatesValidAlgorithm()
    {
        // Arrange
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = ecdsa.ExportParameters(true);

        // Act
        using var algorithm = new AlgorithmEcdsa(parameters, HashAlgorithmName.SHA256);

        // Assert
        Assert.NotNull(algorithm);
        Assert.Equal("ES256", algorithm.Name);
    }

    [Fact]
    public void Sign_ValidData_ReturnsBase64UrlEncodedSignature()
    {
        // Arrange
        using var algorithm = new AlgorithmEcdsa(256, HashAlgorithmName.SHA256);
        var header = "eyJhbGciOiJFUzI1NiJ9";
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
        using var algorithm = new AlgorithmEcdsa(256, HashAlgorithmName.SHA256);
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
        using var algorithm = new AlgorithmEcdsa(256, HashAlgorithmName.SHA256);
        var header = "eyJhbGciOiJFUzI1NiJ9";
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
        using var algorithm = new AlgorithmEcdsa(256, HashAlgorithmName.SHA256);
        var header = "eyJhbGciOiJFUzI1NiJ9";
        var payload = "eyJpc3MiOiJqb2UifQ";
        var signature = algorithm.Sign(header, payload);

        // Create a modified signature by changing some bytes
        var signatureBytes = signature.DecodeBase64Url();
        signatureBytes[0] ^= 0xFF;
        var tamperedSignature = signatureBytes.EncodeBase64Url();

        // Act
        var result = algorithm.VerifySignature(header, payload, tamperedSignature);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifySignature_TamperedPayload_ReturnsFalse()
    {
        // Arrange
        using var algorithm = new AlgorithmEcdsa(256, HashAlgorithmName.SHA256);
        var header = "eyJhbGciOiJFUzI1NiJ9";
        var payload = "eyJpc3MiOiJqb2UifQ";
        var signature = algorithm.Sign(header, payload);
        var tamperedPayload = "eyJpc3MiOiJqYW5lIn0";

        // Act
        var result = algorithm.VerifySignature(header, tamperedPayload, signature);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(GetAllEcdsaAlgorithmCombinations), DisableDiscoveryEnumeration = true)]
    public void SignAndVerify_AllAlgorithmCombinations_WorksCorrectly(int keySize, HashAlgorithmName hashAlgorithm)
    {
        // Arrange
        using var algorithm = new AlgorithmEcdsa(keySize, hashAlgorithm);
        var header = "eyJhbGciOiJFUzI1NiJ9";
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
        var algorithm = new AlgorithmEcdsa(256, HashAlgorithmName.SHA256);

        // Act & Assert - should not throw
        algorithm.Dispose();
        algorithm.Dispose();
    }

    [Fact]
    public void GetJwk_NotImplemented_ThrowsNotImplementedException()
    {
        // Arrange
        using var algorithm = new AlgorithmEcdsa(256, HashAlgorithmName.SHA256);

        // Act & Assert
        Assert.Throws<NotImplementedException>(() => algorithm.GetJwk(false));
    }

    [Theory]
    [InlineData("nistP256", "ES256")]
    [InlineData("nistP384", "ES384")]
    [InlineData("nistP521", "ES512")]
    public void Constructor_WithDifferentCurves_CreatesCorrectAlgorithm(string curveName, string expectedAlgName)
    {
        // Arrange
        var curve = curveName switch
        {
            "nistP256" => ECCurve.NamedCurves.nistP256,
            "nistP384" => ECCurve.NamedCurves.nistP384,
            "nistP521" => ECCurve.NamedCurves.nistP521,
            _ => throw new ArgumentException($"Unknown curve: {curveName}")
        };

        var hashAlgorithm = curveName switch
        {
            "nistP256" => HashAlgorithmName.SHA256,
            "nistP384" => HashAlgorithmName.SHA384,
            "nistP521" => HashAlgorithmName.SHA512,
            _ => throw new ArgumentException($"Unknown curve: {curveName}")
        };

        using var cert = CertificateHelper.CreateSelfSignedEcdsaCertificate(curve);

        // Act
        using var algorithm = new AlgorithmEcdsa(cert, hashAlgorithm);

        // Assert
        Assert.Equal(expectedAlgName, algorithm.Name);
    }

    public static TheoryData<int, HashAlgorithmName> GetAllEcdsaAlgorithmCombinations()
    {
        var data = new TheoryData<int, HashAlgorithmName>
        {
            { 256, HashAlgorithmName.SHA256 },
            { 384, HashAlgorithmName.SHA384 },
            { 521, HashAlgorithmName.SHA512 },
        };

        return data;
    }

    private static HashAlgorithmName GetHashAlgorithmForKeySize(int keySize)
    {
        return keySize switch
        {
            256 => HashAlgorithmName.SHA256,
            384 => HashAlgorithmName.SHA384,
            521 => HashAlgorithmName.SHA512,
            _ => HashAlgorithmName.SHA256
        };
    }
}
