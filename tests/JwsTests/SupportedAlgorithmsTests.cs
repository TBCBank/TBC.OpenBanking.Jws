// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests;

using System.Security.Cryptography;
using TBC.OpenBanking.Jws.Tests.Helpers;

/// <summary>
/// Unit tests for the <see cref="SupportedAlgorithms"/> class.
/// Tests algorithm creation and support checking functionality.
/// </summary>
public class SupportedAlgorithmsTests
{
    [Theory]
    [InlineData(SupportedAlgorithms.RsaPKCS1Sha256, true)]
    [InlineData(SupportedAlgorithms.RsaPKCS1Sha384, true)]
    [InlineData(SupportedAlgorithms.RsaPKCS1Sha512, true)]
    [InlineData(SupportedAlgorithms.RsaSsaPssSha256, true)]
    [InlineData(SupportedAlgorithms.RsaSsaPssSha384, true)]
    [InlineData(SupportedAlgorithms.RsaSsaPssSha512, true)]
    [InlineData(SupportedAlgorithms.EcdsaSha256, true)]
    [InlineData(SupportedAlgorithms.EcdsaSha384, true)]
    [InlineData(SupportedAlgorithms.EcdsaSha512, true)]
    [InlineData("HS256", false)]
    [InlineData("none", false)]
    [InlineData("UNKNOWN", false)]
    [InlineData("", false)]
    public void IsSupportedAlgorithm_ReturnsCorrectResult(string algorithm, bool expectedResult)
    {
        // Arrange & Act
        var result = SupportedAlgorithms.IsSupportedAlgorithm(algorithm);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(SupportedAlgorithms.RsaPKCS1Sha256)]
    [InlineData(SupportedAlgorithms.RsaPKCS1Sha384)]
    [InlineData(SupportedAlgorithms.RsaPKCS1Sha512)]
    [InlineData(SupportedAlgorithms.RsaSsaPssSha256)]
    [InlineData(SupportedAlgorithms.RsaSsaPssSha384)]
    [InlineData(SupportedAlgorithms.RsaSsaPssSha512)]
    public void CreateSigner_RsaAlgorithms_ReturnsValidSigner(string algorithm)
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();

        // Act
        var signer = SupportedAlgorithms.CreateSigner(cert, algorithm);

        // Assert
        Assert.NotNull(signer);

        // Cleanup
        (signer as IDisposable)?.Dispose();
    }

    [Theory]
    [InlineData(SupportedAlgorithms.EcdsaSha256, "nistP256")]
    [InlineData(SupportedAlgorithms.EcdsaSha384, "nistP384")]
    [InlineData(SupportedAlgorithms.EcdsaSha512, "nistP521")]
    public void CreateSigner_EcdsaAlgorithms_ReturnsValidSigner(string algorithm, string curveName)
    {
        // Arrange
        var curve = curveName switch
        {
            "nistP256" => ECCurve.NamedCurves.nistP256,
            "nistP384" => ECCurve.NamedCurves.nistP384,
            "nistP521" => ECCurve.NamedCurves.nistP521,
            _ => throw new ArgumentException($"Unknown curve: {curveName}")
        };

        using var cert = CertificateHelper.CreateSelfSignedEcdsaCertificate(curve);

        // Act
        var signer = SupportedAlgorithms.CreateSigner(cert, algorithm);

        // Assert
        Assert.NotNull(signer);

        // Cleanup
        (signer as IDisposable)?.Dispose();
    }

    [Fact]
    public void CreateSigner_NullCertificate_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            SupportedAlgorithms.CreateSigner(null!, SupportedAlgorithms.RsaPKCS1Sha256));
    }

    [Fact]
    public void CreateSigner_CertificateWithoutPrivateKey_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var certWithPrivateKey = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var certWithoutPrivateKey = CertificateHelper.GetPublicKeyOnlyCertificate(certWithPrivateKey);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SupportedAlgorithms.CreateSigner(certWithoutPrivateKey, SupportedAlgorithms.RsaPKCS1Sha256));
        Assert.Contains("Private key is missing", ex.Message);
    }

    [Fact]
    public void CreateSigner_UnsupportedAlgorithm_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SupportedAlgorithms.CreateSigner(cert, "UNKNOWN"));
        Assert.Contains("Unsupported algorithm", ex.Message);
    }

    [Theory]
    [InlineData(SupportedAlgorithms.RsaPKCS1Sha256)]
    [InlineData(SupportedAlgorithms.RsaPKCS1Sha384)]
    [InlineData(SupportedAlgorithms.RsaPKCS1Sha512)]
    [InlineData(SupportedAlgorithms.RsaSsaPssSha256)]
    [InlineData(SupportedAlgorithms.RsaSsaPssSha384)]
    [InlineData(SupportedAlgorithms.RsaSsaPssSha512)]
    public void CreateVerifier_RsaAlgorithms_ReturnsValidVerifier(string algorithm)
    {
        // Arrange
        using var certWithPrivateKey = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var cert = CertificateHelper.GetPublicKeyOnlyCertificate(certWithPrivateKey);

        // Act
        using var verifier = SupportedAlgorithms.CreateVerifier(cert, algorithm);

        // Assert
        Assert.NotNull(verifier);
    }

    [Theory]
    [InlineData(SupportedAlgorithms.EcdsaSha256, "nistP256")]
    [InlineData(SupportedAlgorithms.EcdsaSha384, "nistP384")]
    [InlineData(SupportedAlgorithms.EcdsaSha512, "nistP521")]
    public void CreateVerifier_EcdsaAlgorithms_ReturnsValidVerifier(string algorithm, string curveName)
    {
        // Arrange
        var curve = curveName switch
        {
            "nistP256" => ECCurve.NamedCurves.nistP256,
            "nistP384" => ECCurve.NamedCurves.nistP384,
            "nistP521" => ECCurve.NamedCurves.nistP521,
            _ => throw new ArgumentException($"Unknown curve: {curveName}")
        };

        // Note: AlgorithmEcdsa requires a certificate with private key even for verification,
        // as it checks HasPrivateKey in the constructor
        using var cert = CertificateHelper.CreateSelfSignedEcdsaCertificate(curve);

        // Act
        using var verifier = SupportedAlgorithms.CreateVerifier(cert, algorithm);

        // Assert
        Assert.NotNull(verifier);
    }

    [Fact]
    public void CreateVerifier_UnsupportedAlgorithm_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var certWithPrivateKey = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var cert = CertificateHelper.GetPublicKeyOnlyCertificate(certWithPrivateKey);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            SupportedAlgorithms.CreateVerifier(cert, "UNKNOWN"));
        Assert.Contains("Unsupported algorithm", ex.Message);
    }

    [Fact]
    public void IsSupportedAlgorithm_CaseInsensitive_ReturnsTrue()
    {
        // Arrange & Act & Assert
        Assert.True(SupportedAlgorithms.IsSupportedAlgorithm("rs256"));
        Assert.True(SupportedAlgorithms.IsSupportedAlgorithm("RS256"));
        Assert.True(SupportedAlgorithms.IsSupportedAlgorithm("Rs256"));
    }

    [Fact]
    public void CreateSigner_SignsDataCorrectly()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var signer = (Algorithm)SupportedAlgorithms.CreateSigner(cert, SupportedAlgorithms.RsaPKCS1Sha256);
        var data = "test data"u8.ToArray();

        // Act
        var signature = signer.SignData(data);

        // Assert
        Assert.NotNull(signature);
        Assert.NotEmpty(signature);
    }
}
