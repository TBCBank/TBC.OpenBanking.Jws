// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests;

using System.Security.Cryptography.X509Certificates;
using TBC.OpenBanking.Jws.Tests.Helpers;

/// <summary>
/// Unit tests for the <see cref="ProtectedHeader"/> class.
/// Tests JWS protected header creation and manipulation.
/// </summary>
public class ProtectedHeaderTests
{
    [Fact]
    public void Constructor_DefaultValues_SetsExpectedProperties()
    {
        // Arrange & Act
        var header = new ProtectedHeader();

        // Assert
        Assert.False(header.EncodeToBeSignedData);
        Assert.NotNull(header.EncodedCertificates);
        Assert.Empty(header.EncodedCertificates);
        Assert.NotNull(header.CriticalHeaderNames);
        Assert.Contains("sigT", header.CriticalHeaderNames);
        Assert.Contains("sigD", header.CriticalHeaderNames);
        Assert.Contains("b64", header.CriticalHeaderNames);
        Assert.NotNull(header.DataToBeSigned);
    }

    [Fact]
    public void SignatureTime_DefaultValue_IsCloseToUtcNow()
    {
        // Arrange & Act
        var header = new ProtectedHeader();

        // Assert
        var timeDifference = DateTime.UtcNow - header.SignatureTime;
        Assert.True(timeDifference.TotalSeconds < 5);
    }

    [Fact]
    public void DataToBeSigned_DefaultIdentificationMechanism_IsCorrect()
    {
        // Arrange & Act
        var header = new ProtectedHeader();

        // Assert
        Assert.Equal("http://uri.etsi.org/19182/HttpHeaders", header.DataToBeSigned.IdentificationMechanism);
    }

    [Fact]
    public void DataToBeSigned_AddParameter_AddsParameterToList()
    {
        // Arrange
        var header = new ProtectedHeader();

        // Act
        header.DataToBeSigned.AddParameter("content-type");
        header.DataToBeSigned.AddParameter("host");

        // Assert
        Assert.Contains("content-type", header.DataToBeSigned.Parameters);
        Assert.Contains("host", header.DataToBeSigned.Parameters);
    }

    [Fact]
    public void DataToBeSigned_AddParameter_LowercasesValue()
    {
        // Arrange
        var header = new ProtectedHeader();

        // Act
        header.DataToBeSigned.AddParameter("Content-Type");

        // Assert
        Assert.Contains("content-type", header.DataToBeSigned.Parameters);
        Assert.DoesNotContain("Content-Type", header.DataToBeSigned.Parameters);
    }

    [Fact]
    public void DataToBeSigned_AddParameter_PreventsDuplication()
    {
        // Arrange
        var header = new ProtectedHeader();

        // Act
        header.DataToBeSigned.AddParameter("content-type");
        header.DataToBeSigned.AddParameter("Content-Type");
        header.DataToBeSigned.AddParameter("CONTENT-TYPE");

        // Assert
        Assert.Single(header.DataToBeSigned.Parameters);
    }

    [Fact]
    public void DataToBeSigned_AddParameter_EmptyString_DoesNotAdd()
    {
        // Arrange
        var header = new ProtectedHeader();

        // Act
        header.DataToBeSigned.AddParameter(string.Empty);

        // Assert
        Assert.Empty(header.DataToBeSigned.Parameters);
    }

    [Fact]
    public void DataToBeSigned_AddParameter_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var header = new ProtectedHeader();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => header.DataToBeSigned.AddParameter(null!));
    }

    [Fact]
    public void EncodeCertificate_ValidCertificate_ReturnsBase64String()
    {
        // Arrange
        var header = new ProtectedHeader();
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate();

        // Act
        var encoded = header.EncodeCertificate(cert);

        // Assert
        Assert.NotNull(encoded);
        Assert.NotEmpty(encoded);
        // Verify it's valid Base64
        var bytes = Convert.FromBase64String(encoded);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void EncodeCertificate_NullCertificate_ThrowsArgumentNullException()
    {
        // Arrange
        var header = new ProtectedHeader();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => header.EncodeCertificate(null!));
    }

    [Fact]
    public void DecodeCertificate_ValidEncodedCertificate_ReturnsCertificate()
    {
        // Arrange
        var header = new ProtectedHeader();
        using var originalCert = CertificateHelper.CreateSelfSignedRsaCertificate();
        var encoded = header.EncodeCertificate(originalCert);

        // Act
        using var decodedCert = header.DecodeCertificate(encoded);

        // Assert
        Assert.NotNull(decodedCert);
        Assert.Equal(originalCert.Thumbprint, decodedCert.Thumbprint);
    }

    [Fact]
    public void SetEncodedCertificates_WithSignerAndChain_SetsCorrectOrder()
    {
        // Arrange
        var header = new ProtectedHeader();
        var (endCert, chain) = CertificateHelper.CreateCertificateChain(isRsa: true);

        // Act
        header.SetEncodedCertificates(endCert, chain);

        // Assert
        Assert.Equal(2, header.EncodedCertificates.Count);

        // First certificate should be the signer certificate
        using var firstCert = header.DecodeCertificate(header.EncodedCertificates[0]);
        Assert.Equal(endCert.Thumbprint, firstCert.Thumbprint);

        // Cleanup
        endCert.Dispose();
        foreach (var c in chain)
        {
            c.Dispose();
        }
    }

    [Fact]
    public void SetEncodedCertificates_WithSignerOnly_SetsOneCertificate()
    {
        // Arrange
        var header = new ProtectedHeader();
        using var signerCert = CertificateHelper.CreateSelfSignedRsaCertificate();

        // Act
        header.SetEncodedCertificates(signerCert, null);

        // Assert
        Assert.Single(header.EncodedCertificates);
    }

    [Fact]
    public void SetEncodedCertificates_CalledMultipleTimes_ClearsOldCertificates()
    {
        // Arrange
        var header = new ProtectedHeader();
        using var cert1 = CertificateHelper.CreateSelfSignedRsaCertificate();
        using var cert2 = CertificateHelper.CreateSelfSignedRsaCertificate();

        // Act
        header.SetEncodedCertificates(cert1, null);
        var countAfterFirst = header.EncodedCertificates.Count;
        header.SetEncodedCertificates(cert2, null);
        var countAfterSecond = header.EncodedCertificates.Count;

        // Assert
        Assert.Equal(1, countAfterFirst);
        Assert.Equal(1, countAfterSecond);

        using var decodedCert = header.DecodeCertificate(header.EncodedCertificates[0]);
        Assert.Equal(cert2.Thumbprint, decodedCert.Thumbprint);
    }

    [Fact]
    public void AlgorithmName_CanBeSetAndRetrieved()
    {
        // Arrange
        var header = new ProtectedHeader();

        // Act
        header.AlgorithmName = SupportedAlgorithms.RsaPKCS1Sha256;

        // Assert
        Assert.Equal(SupportedAlgorithms.RsaPKCS1Sha256, header.AlgorithmName);
    }

    [Fact]
    public void SignatureTime_CanBeSetAndRetrieved()
    {
        // Arrange
        var header = new ProtectedHeader();
        var specificTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        // Act
        header.SignatureTime = specificTime;

        // Assert
        Assert.Equal(specificTime, header.SignatureTime);
    }

    [Fact]
    public void EncodeToBeSignedData_CanBeSetAndRetrieved()
    {
        // Arrange
        var header = new ProtectedHeader();

        // Act
        header.EncodeToBeSignedData = true;

        // Assert
        Assert.True(header.EncodeToBeSignedData);
    }

    [Fact]
    public void CriticalHeaderNames_CanBeModified()
    {
        // Arrange
        var header = new ProtectedHeader();

        // Act
        header.CriticalHeaderNames.Add("custom-header");

        // Assert
        Assert.Contains("custom-header", header.CriticalHeaderNames);
    }

    [Fact]
    public void EncodedCertificates_RoundTrip_PreservesData()
    {
        // Arrange
        var header = new ProtectedHeader();
        using var originalCert = CertificateHelper.CreateSelfSignedEcdsaCertificate();

        // Act
        var encoded = header.EncodeCertificate(originalCert);
        using var decodedCert = header.DecodeCertificate(encoded);

        // Assert
        Assert.Equal(originalCert.Subject, decodedCert.Subject);
        Assert.Equal(originalCert.Issuer, decodedCert.Issuer);
        Assert.Equal(originalCert.SerialNumber, decodedCert.SerialNumber);
    }
}
