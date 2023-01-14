/*
 * Copyright (c) 2021 JSC TBC Bank
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace TBC.OpenBanking.Jws;

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using TBC.OpenBanking.Jws.Exceptions;

/// <summary>
/// This Protected header corresponds to some exceptions and extensions from local (Georgian) OpenBanking standard
/// </summary>
public class ProtectedHeader
{
    public class DataToBeSignedDescription
    {
        // Properties must have public setters for System.Text.Json to be able to deserialize them

        [JsonPropertyName("pars")]
        public List<string> Parameters { get; set; } = new List<string>();

        [JsonPropertyName("mId")]
        public string IdentificationMechanism { get; set; } = "http://uri.etsi.org/19182/HttpHeaders";

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Intentional")]
        public void AddParameter(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (value.Length != 0)
            {
                // Preventing duplication
                if (!Parameters.Contains(value, StringComparer.OrdinalIgnoreCase))
                    Parameters.Add(value.ToLowerInvariant());
            }
        }
    }

    public ProtectedHeader()
    {
        EncodeToBeSignedData = false;
        EncodedCertificates = new List<string>();
        CriticalHeaderNames = new List<string>(3) { "sigT", "sigD", "b64" };
        SignatureTime = DateTime.UtcNow;
    }

    [JsonPropertyName("b64")]
    public bool EncodeToBeSignedData { get; set; }

    /// <summary>
    /// <see href="https://tools.ietf.org/html/rfc7515#section-4.1.6"/>
    /// </summary>
    [JsonPropertyName("x5c")]
    public List<string> EncodedCertificates { get; set; }

    [JsonPropertyName("crit")]
    public List<string> CriticalHeaderNames { get; set; }

    /// <summary>
    /// Contains signature creation UTC time.
    /// </summary>
    [JsonPropertyName("sigT")]
    [JsonConverter(typeof(DateFormatConverter))]
    public DateTime SignatureTime { get; set; }

    [JsonPropertyName("sigD")]
    public DataToBeSignedDescription DataToBeSigned { get; set; } = new DataToBeSignedDescription();

    [JsonPropertyName("alg")]
    public string AlgorithmName { get; set; }

    /// <summary>
    /// Decode Base64-encoded certificate.
    /// </summary>
    /// <param name="encodedCertificate">Base64-encoded byte array representing X.509 certificate.</param>
    /// <returns></returns>
    public X509Certificate2 DecodeCertificate(string encodedCertificate)
    {
        byte[] rawData = Convert.FromBase64String(encodedCertificate);
        return new X509Certificate2(rawData);
    }

    /// <summary>
    /// Encode <paramref name="cert"/> to Base64. Only one certificate and no private key will be exported.
    /// </summary>
    /// <param name="cert"></param>
    /// <returns></returns>
    public string EncodeCertificate(X509Certificate2 cert)
    {
        _ = cert ?? throw new ArgumentNullException(nameof(cert));

        byte[] rawData = cert.Export(X509ContentType.Cert);
        return Convert.ToBase64String(rawData);
    }

    /// <summary>
    /// Populate encoded certificates collection. Any other data from EncodedCertificates will be deleted first.
    /// According to <see href="https://tools.ietf.org/html/rfc7515#section-4.1.6"/>, first one should be
    /// the signer certificate and then chain elements.
    /// </summary>
    /// <param name="signerCertificate"></param>
    /// <param name="chainCertificates">Represents chain of certificates except root certificate. Can be null or empty</param>
    public void SetEncodedCertificates(X509Certificate2 signerCertificate, X509Certificate2Collection chainCertificates)
    {
        EncodedCertificates.Clear();
        EncodedCertificates.Add(EncodeCertificate(signerCertificate));

        if (chainCertificates != null)
        {
            foreach (var cert in chainCertificates)
            {
                EncodedCertificates.Add(EncodeCertificate(cert));
            }
        }
    }

    public void SetEncodedCertificates(X509Certificate2 cert)
    {
        EncodedCertificates.Clear();

        using var chain = new X509Chain();

        chain.ChainPolicy = new X509ChainPolicy()
        {
            RevocationMode = X509RevocationMode.NoCheck,
            VerificationFlags = X509VerificationFlags.AllFlags,
            //DisableCertificateDownloads = true,
            //TrustMode = X509ChainTrustMode.CustomRootTrust
        };

        if (!chain.Build(cert))
            throw new CertificateValidationException(chain.ChainStatus, "Chain build error");

        foreach (var element in chain.ChainElements)
        {
            EncodedCertificates.Add(EncodeCertificate(element.Certificate));
        }
    }
}
