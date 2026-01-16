// Copyright ⓒ TBC Bank. All rights reserved.

namespace TBC.OpenBanking.Jws.Tests.Helpers;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

/// <summary>
/// Helper class for generating self-signed X509 certificates for unit tests.
/// All certificates are kept in memory and are not installed into any certificate store.
/// </summary>
internal static class CertificateHelper
{
    /// <summary>
    /// Creates a self-signed RSA certificate with a private key suitable for signing.
    /// </summary>
    /// <param name="keySize">The RSA key size in bits. Defaults to 2048.</param>
    /// <param name="subjectName">The certificate subject name. Defaults to "CN=TestCert".</param>
    /// <param name="organizationIdentifier">The organization identifier to include in the subject. Optional.</param>
    /// <returns>A self-signed X509Certificate2 with private key.</returns>
    public static X509Certificate2 CreateSelfSignedRsaCertificate(
        int keySize = 2048,
        string subjectName = "CN=TestCert",
        string? organizationIdentifier = null)
    {
        using var rsa = RSA.Create(keySize);

        var subject = organizationIdentifier is not null
            ? $"{subjectName}, OID.2.5.4.97={organizationIdentifier}"
            : subjectName;

        var request = new CertificateRequest(
            subject,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Add basic constraints (not a CA)
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, true));

        // Add key usage for digital signature
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                true));

        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = DateTimeOffset.UtcNow.AddYears(1);

        var certificate = request.CreateSelfSigned(notBefore, notAfter);

        // Export and re-import to ensure the private key is exportable on all platforms
        var pfxBytes = certificate.Export(X509ContentType.Pfx, "");

#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadPkcs12(pfxBytes, "", X509KeyStorageFlags.Exportable);
#else
        return new X509Certificate2(pfxBytes, "", X509KeyStorageFlags.Exportable);
#endif
    }

    /// <summary>
    /// Creates a self-signed ECDSA certificate with a private key suitable for signing.
    /// </summary>
    /// <param name="curve">The ECCurve to use. Defaults to P-256 (NIST P-256).</param>
    /// <param name="subjectName">The certificate subject name. Defaults to "CN=TestCertEcdsa".</param>
    /// <param name="organizationIdentifier">The organization identifier to include in the subject. Optional.</param>
    /// <returns>A self-signed X509Certificate2 with private key.</returns>
    public static X509Certificate2 CreateSelfSignedEcdsaCertificate(
        ECCurve? curve = null,
        string subjectName = "CN=TestCertEcdsa",
        string? organizationIdentifier = null)
    {
        curve ??= ECCurve.NamedCurves.nistP256;

        using var ecdsa = ECDsa.Create(curve.Value);

        var subject = organizationIdentifier is not null
            ? $"{subjectName}, OID.2.5.4.97={organizationIdentifier}"
            : subjectName;

        var request = new CertificateRequest(
            subject,
            ecdsa,
            HashAlgorithmName.SHA256);

        // Add basic constraints (not a CA)
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, true));

        // Add key usage for digital signature
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));

        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        var notAfter = DateTimeOffset.UtcNow.AddYears(1);

        var certificate = request.CreateSelfSigned(notBefore, notAfter);

        // Export and re-import to ensure the private key is exportable on all platforms
        var pfxBytes = certificate.Export(X509ContentType.Pfx, "");

#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadPkcs12(pfxBytes, "", X509KeyStorageFlags.Exportable);
#else
        return new X509Certificate2(pfxBytes, "", X509KeyStorageFlags.Exportable);
#endif
    }

    /// <summary>
    /// Creates a certificate chain with a root CA and an end-entity certificate.
    /// </summary>
    /// <param name="isRsa">If true, creates RSA certificates; otherwise creates ECDSA certificates.</param>
    /// <param name="organizationIdentifier">The organization identifier for the end certificate.</param>
    /// <returns>A tuple containing the end certificate (with private key) and the certificate chain collection.</returns>
    public static (X509Certificate2 EndCertificate, X509Certificate2Collection Chain) CreateCertificateChain(
        bool isRsa = true,
        string? organizationIdentifier = null)
    {
        // Create root CA
        X509Certificate2 rootCert;
        X509Certificate2 endCert;

        if (isRsa)
        {
            using var rootRsa = RSA.Create(2048);
            var rootRequest = new CertificateRequest(
                "CN=TestRootCA",
                rootRsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            rootRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, true, 1, true));
            rootRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                    true));

            rootCert = rootRequest.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddYears(10));

            // Create end-entity certificate signed by root
            using var endRsa = RSA.Create(2048);

            var endSubject = organizationIdentifier is not null
                ? $"CN=TestEndEntity, OID.2.5.4.97={organizationIdentifier}"
                : "CN=TestEndEntity";

            var endRequest = new CertificateRequest(
                endSubject,
                endRsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            endRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));
            endRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    true));

            var serialNumber = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(serialNumber);
            }

            endCert = endRequest.Create(
                rootCert,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddYears(1),
                serialNumber);

            // Combine with private key
            endCert = endCert.CopyWithPrivateKey(endRsa);
        }
        else
        {
            using var rootEcdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var rootRequest = new CertificateRequest(
                "CN=TestRootCA",
                rootEcdsa,
                HashAlgorithmName.SHA256);

            rootRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(true, true, 1, true));
            rootRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                    true));

            rootCert = rootRequest.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddYears(10));

            // Create end-entity certificate signed by root
            using var endEcdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var endSubject = organizationIdentifier is not null
                ? $"CN=TestEndEntity, OID.2.5.4.97={organizationIdentifier}"
                : "CN=TestEndEntity";

            var endRequest = new CertificateRequest(
                endSubject,
                endEcdsa,
                HashAlgorithmName.SHA256);

            endRequest.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, true));
            endRequest.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));

            var serialNumber = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(serialNumber);
            }

            endCert = endRequest.Create(
                rootCert,
                DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddYears(1),
                serialNumber);

            // Combine with private key
            endCert = endCert.CopyWithPrivateKey(endEcdsa);
        }

        // Export and re-import to make the private key exportable
        var pfxBytes = endCert.Export(X509ContentType.Pfx, "");

#if NET9_0_OR_GREATER
        var exportableEndCert = X509CertificateLoader.LoadPkcs12(pfxBytes, "", X509KeyStorageFlags.Exportable);
#else
        var exportableEndCert = new X509Certificate2(pfxBytes, "", X509KeyStorageFlags.Exportable);
#endif

        var chain = new X509Certificate2Collection { rootCert };

        return (exportableEndCert, chain);
    }

    /// <summary>
    /// Creates a public-key-only certificate from an existing certificate (strips the private key).
    /// </summary>
    /// <param name="certificate">The certificate to extract the public key from.</param>
    /// <returns>A new certificate containing only the public key.</returns>
    public static X509Certificate2 GetPublicKeyOnlyCertificate(X509Certificate2 certificate)
    {
        var publicKeyBytes = certificate.Export(X509ContentType.Cert);

#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadCertificate(publicKeyBytes);
#else
        return new X509Certificate2(publicKeyBytes);
#endif
    }
}
