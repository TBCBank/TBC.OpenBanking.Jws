// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests;

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using TBC.OpenBanking.Jws.Exceptions;
using TBC.OpenBanking.Jws.Tests.Helpers;

/// <summary>
/// Unit tests for the <see cref="HttpSignatureVerifier{T}"/> class.
/// Tests HTTP request/response signature verification functionality.
/// </summary>
public class HttpSignatureVerifierTests
{
    [Fact]
    public void Constructor_WithLogger_CreatesInstance()
    {
        // Arrange & Act
        var verifier = new HttpSignatureVerifier<HttpRequestData>(NullLogger<HttpSignatureVerifier<HttpRequestData>>.Instance);

        // Assert
        Assert.NotNull(verifier);
        Assert.False(verifier.IsSignatureVerified);
        Assert.Null(verifier.ProtectedHeader);
        Assert.True(verifier.CheckSignatureTimeConstraint);
    }

    [Fact]
    public void Constructor_WithNullLogger_CreatesInstanceWithNullLogger()
    {
        // Arrange & Act
        var verifier = new HttpSignatureVerifier<HttpRequestData>(null!);

        // Assert
        Assert.NotNull(verifier);
    }

    [Fact]
    public void CheckSignatureTimeConstraint_CanBeSetAndRetrieved()
    {
        // Arrange
        var verifier = new HttpSignatureVerifier<HttpRequestData>(NullLogger<HttpSignatureVerifier<HttpRequestData>>.Instance);

        // Act
        verifier.CheckSignatureTimeConstraint = false;

        // Assert
        Assert.False(verifier.CheckSignatureTimeConstraint);
    }

    [Fact]
    public void CertificateValidationFlags_CanBeSetAndRetrieved()
    {
        // Arrange
        var verifier = new HttpSignatureVerifier<HttpRequestData>(NullLogger<HttpSignatureVerifier<HttpRequestData>>.Instance);
        var flags = new CertificateValidationFlags
        {
            RevocationMode = X509RevocationMode.NoCheck,
            VerificationFlags = X509VerificationFlags.AllFlags
        };

        // Act
        verifier.CertificateValidationFlags = flags;

        // Assert
        Assert.Same(flags, verifier.CertificateValidationFlags);
    }

    [Fact]
    public void VerifySignature_WithNullHttpData_ThrowsArgumentNullException()
    {
        // Arrange
        var verifier = new HttpSignatureVerifier<HttpRequestData>(NullLogger<HttpSignatureVerifier<HttpRequestData>>.Instance);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => verifier.VerifySignature(null!, DateTime.UtcNow));
    }

    [Fact]
    public void VerifySignature_MissingSignatureHeader_ThrowsHeaderMissingException()
    {
        // Arrange
        var verifier = new HttpSignatureVerifier<HttpRequestData>(NullLogger<HttpSignatureVerifier<HttpRequestData>>.Instance);
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api"),
            Method = "POST",
            Body = "test"u8.ToArray()
        };
        requestData.AddHeader("host", "example.com");
        requestData.AddHeader("x-request-id", "123");
        requestData.AddHeader("digest", "SHA-256=test");
        // Missing x-jws-signature header

        // Act & Assert
        var ex = Assert.Throws<HeaderMissingException>(() =>
            verifier.VerifySignature(requestData, DateTime.UtcNow));
        Assert.Contains("x-jws-signature", ex.Message);
    }

    [Fact]
    public void VerifySignature_SignedRequestWithSelfSignedCert_ThrowsCertificateValidationException()
    {
        // Arrange
        // Create a signed request using HttpSigner
        // Note: Self-signed certificates will fail chain validation by design in this library.
        // The library throws CertificateValidationException for any chain status that is not NoError,
        // even if VerificationFlags includes AllowUnknownCertificateAuthority.
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate(organizationIdentifier: "PSDGE-NBG-123456");
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

        var requestData = CreateValidHttpRequestData("PSDGE-NBG-123456");
        signer.CreateSignature(requestData);

        // Add signature and digest headers to request
        requestData.Headers["x-jws-signature"] = signer.SignatureHeaderValue!;
        requestData.Headers["digest"] = signer.DigestHeaderValue!;

        // Setup verifier
        var verifier = new HttpSignatureVerifier<HttpRequestData>(NullLogger<HttpSignatureVerifier<HttpRequestData>>.Instance)
        {
            CheckSignatureTimeConstraint = false,
            CertificateValidationFlags = new CertificateValidationFlags
            {
                RevocationMode = X509RevocationMode.NoCheck,
                VerificationFlags = X509VerificationFlags.AllFlags
            }
        };

        // Act & Assert
        // Self-signed certificates will fail certificate chain validation
        var ex = Assert.Throws<CertificateValidationException>(() =>
            verifier.VerifySignature(requestData, DateTime.UtcNow));
        Assert.Contains("UntrustedRoot", ex.Message);
    }

    [Fact]
    public void VerifySignature_TamperedBody_ThrowsSignatureVerificationProblemException()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate(organizationIdentifier: "PSDGE-NBG-123456");
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

        var requestData = CreateValidHttpRequestData("PSDGE-NBG-123456");
        signer.CreateSignature(requestData);

        // Add signature and digest headers
        requestData.Headers["x-jws-signature"] = signer.SignatureHeaderValue!;
        requestData.Headers["digest"] = signer.DigestHeaderValue!;

        // Tamper with the body
        requestData.Body = "{ \"tampered\": true }"u8.ToArray();

        var verifier = new HttpSignatureVerifier<HttpRequestData>(NullLogger<HttpSignatureVerifier<HttpRequestData>>.Instance)
        {
            CheckSignatureTimeConstraint = false,
            CertificateValidationFlags = new CertificateValidationFlags
            {
                RevocationMode = X509RevocationMode.NoCheck,
                VerificationFlags = X509VerificationFlags.AllFlags
            }
        };

        // Act & Assert
        // Tampered body causes digest mismatch which is detected before certificate chain validation
        var ex = Assert.Throws<SignatureVerificationProblemException>(() =>
            verifier.VerifySignature(requestData, DateTime.UtcNow));
        Assert.Contains("Digest mismatch", ex.Message);
    }

    [Fact]
    public void VerifySignature_SignatureTimeTooFarInFuture_ThrowsException()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate(organizationIdentifier: "PSDGE-NBG-123456");
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

        var requestData = CreateValidHttpRequestData("PSDGE-NBG-123456");
        signer.CreateSignature(requestData);

        requestData.Headers["x-jws-signature"] = signer.SignatureHeaderValue!;
        requestData.Headers["digest"] = signer.DigestHeaderValue!;

        var verifier = new HttpSignatureVerifier<HttpRequestData>(NullLogger<HttpSignatureVerifier<HttpRequestData>>.Instance)
        {
            CheckSignatureTimeConstraint = true,
            CertificateValidationFlags = new CertificateValidationFlags
            {
                RevocationMode = X509RevocationMode.NoCheck,
                VerificationFlags = X509VerificationFlags.AllFlags
            }
        };

        // Check time is 10 seconds in the past (signature time is in the future from perspective of checkTime)
        var checkTime = DateTime.UtcNow.AddSeconds(-10);

        // Act & Assert
        // Time constraint is checked before certificate validation
        var ex = Assert.Throws<SignatureVerificationProblemException>(() =>
            verifier.VerifySignature(requestData, checkTime));
        Assert.Contains("greater then current time", ex.Message);
    }

    [Fact]
    public void VerifySignature_SignatureTimeTooOld_ThrowsException()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate(organizationIdentifier: "PSDGE-NBG-123456");
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

        var requestData = CreateValidHttpRequestData("PSDGE-NBG-123456");
        signer.CreateSignature(requestData);

        requestData.Headers["x-jws-signature"] = signer.SignatureHeaderValue!;
        requestData.Headers["digest"] = signer.DigestHeaderValue!;

        var verifier = new HttpSignatureVerifier<HttpRequestData>(NullLogger<HttpSignatureVerifier<HttpRequestData>>.Instance)
        {
            CheckSignatureTimeConstraint = true,
            CertificateValidationFlags = new CertificateValidationFlags
            {
                RevocationMode = X509RevocationMode.NoCheck,
                VerificationFlags = X509VerificationFlags.AllFlags
            }
        };

        // Check time is 2 minutes in the future (signature is too old from perspective of checkTime)
        var checkTime = DateTime.UtcNow.AddMinutes(2);

        // Act & Assert
        // Time constraint is checked before certificate validation
        // Note: The exact exception depends on timezone conversions in the library (ToLocalTime)
        var ex = Assert.Throws<SignatureVerificationProblemException>(() =>
            verifier.VerifySignature(requestData, checkTime));
        // The exception should be about time constraint violation
        Assert.True(ex.Message.Contains("signing time", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void VerifySignature_HttpResponseData_ThrowsCertificateValidationException()
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
        responseData.AddHeader("content-type", "application/json");

        signer.CreateSignature(responseData);

        responseData.Headers["x-jws-signature"] = signer.SignatureHeaderValue!;
        responseData.Headers["digest"] = signer.DigestHeaderValue!;

        var verifier = new HttpSignatureVerifier<HttpResponseData>(NullLogger<HttpSignatureVerifier<HttpResponseData>>.Instance)
        {
            CheckSignatureTimeConstraint = false,
            CertificateValidationFlags = new CertificateValidationFlags
            {
                RevocationMode = X509RevocationMode.NoCheck,
                VerificationFlags = X509VerificationFlags.AllFlags
            }
        };

        // Act & Assert
        // Self-signed certificates will fail certificate chain validation
        var ex = Assert.Throws<CertificateValidationException>(() =>
            verifier.VerifySignature(responseData, DateTime.UtcNow));
        Assert.Contains("UntrustedRoot", ex.Message);
    }

    [Fact]
    public void VerifySignature_MissingOrganizationIdHeaderForRequest_ThrowsCertificateValidationException()
    {
        // Arrange
        // Note: The organization identifier check happens AFTER certificate chain validation,
        // so with self-signed certificates, we'll get CertificateValidationException first
        using var cert = CertificateHelper.CreateSelfSignedRsaCertificate(organizationIdentifier: "PSDGE-NBG-123456");
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

        // Create request without x-organization-id header
        var requestData = new HttpRequestData
        {
            Uri = new Uri("https://example.com/api/resource"),
            Method = "POST",
            Body = "{ \"key\": \"value\" }"u8.ToArray()
        };
        requestData.AddHeader("host", "example.com");
        requestData.AddHeader("x-request-id", Guid.NewGuid().ToString());
        requestData.AddHeader("content-type", "application/json");
        // Not adding x-organization-id header

        signer.CreateSignature(requestData);
        requestData.Headers["x-jws-signature"] = signer.SignatureHeaderValue!;
        requestData.Headers["digest"] = signer.DigestHeaderValue!;

        var verifier = new HttpSignatureVerifier<HttpRequestData>(NullLogger<HttpSignatureVerifier<HttpRequestData>>.Instance)
        {
            CheckSignatureTimeConstraint = false,
            CertificateValidationFlags = new CertificateValidationFlags
            {
                RevocationMode = X509RevocationMode.NoCheck,
                VerificationFlags = X509VerificationFlags.AllFlags
            }
        };

        // Act & Assert
        // Certificate chain validation happens before organization ID check with self-signed certs
        var ex = Assert.Throws<CertificateValidationException>(() =>
            verifier.VerifySignature(requestData, DateTime.UtcNow));
        Assert.Contains("UntrustedRoot", ex.Message);
    }

    [Fact]
    public void VerifySignature_WithEcdsaCertificate_ThrowsCertificateValidationException()
    {
        // Arrange
        using var cert = CertificateHelper.CreateSelfSignedEcdsaCertificate(
            curve: System.Security.Cryptography.ECCurve.NamedCurves.nistP256,
            organizationIdentifier: "PSDGE-NBG-123456");
        using var algorithm = new AlgorithmEcdsa(cert, System.Security.Cryptography.HashAlgorithmName.SHA256);

        var signer = new HttpSigner<HttpRequestData>(NullLogger<HttpSigner<HttpRequestData>>.Instance)
        {
            Signer = algorithm,
            SignerCertificate = cert,
            SignerCertificateChain = []
        };

        var requestData = CreateValidHttpRequestData("PSDGE-NBG-123456");
        signer.CreateSignature(requestData);

        requestData.Headers["x-jws-signature"] = signer.SignatureHeaderValue!;
        requestData.Headers["digest"] = signer.DigestHeaderValue!;

        var verifier = new HttpSignatureVerifier<HttpRequestData>(NullLogger<HttpSignatureVerifier<HttpRequestData>>.Instance)
        {
            CheckSignatureTimeConstraint = false,
            CertificateValidationFlags = new CertificateValidationFlags
            {
                RevocationMode = X509RevocationMode.NoCheck,
                VerificationFlags = X509VerificationFlags.AllFlags
            }
        };

        // Act & Assert
        // Self-signed certificates will fail certificate chain validation
        var ex = Assert.Throws<CertificateValidationException>(() =>
            verifier.VerifySignature(requestData, DateTime.UtcNow));
        Assert.Contains("UntrustedRoot", ex.Message);
    }

    private static HttpRequestData CreateValidHttpRequestData(string organizationIdentifier)
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
        requestData.AddHeader("x-organization-id", organizationIdentifier);

        return requestData;
    }
}
