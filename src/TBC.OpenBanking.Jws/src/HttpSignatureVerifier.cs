﻿/*
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
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TBC.OpenBanking.Jws.Exceptions;

public class HttpSignatureVerifier<T> where T : HttpMessageData
{
    private readonly ILogger<HttpSignatureVerifier<T>> _logger;
    private X509Certificate2 _signerCertificate;

    public HttpSignatureVerifier(ILogger<HttpSignatureVerifier<T>> logger)
    {
        _logger = logger ?? NullLoggerFactory.Instance.CreateLogger<HttpSignatureVerifier<T>>();
        InitData();
    }

    private void InitData()
    {
        _signerCertificate = null;
        ProtectedHeader = null;
        IsSignatureVerified = false;
        CertificateValidationFlags = new CertificateValidationFlags();
    }

    public bool CheckSignatureTimeConstraint { get; set; } = true;

    public CertificateValidationFlags CertificateValidationFlags { get; set; }

    public ProtectedHeader ProtectedHeader { get; private set; }

    public bool IsSignatureVerified { get; private set; }

    /// <summary>
    /// All data we need to verify signature is in <paramref name="httpData"/> parameter
    /// </summary>
    /// <param name="httpData">Contains data from HTTP request</param>
    /// <param name="checkTime">Time when request was received. This time is used to check time span constraint, which is 2 seconds.</param>
    /// <returns></returns>
    public bool VerifySignature(T httpData, DateTime checkTime)
    {
        _ = httpData ?? throw new ArgumentNullException(nameof(httpData));

        IsSignatureVerified = false;

        // Check headers. If any is missing throws exception
        CheckMandatoryHeaders(httpData);

        // Get signature header value
        var signatureHeaderValue = httpData.Headers[HttpMessageData.SignatureHeaderName];

        // Signature header value have 2 parts: encoded protection headers and encoded signature
        var signatureParts = signatureHeaderValue.Split(HttpMessageData.SignatureSplitter, StringSplitOptions.RemoveEmptyEntries);

        var encodedProtectedHeader = signatureParts[0]; //contains encoded protected header
        var encodedSignature = signatureParts[1]; //contains encoded signature

        // decode and deserialize protected header
        var jsonProtHeader = UTF8EncodingSealed.Instance.GetString(encodedProtectedHeader.DecodeBase64Url());
        ProtectedHeader = Helper.DeserializeFromJson<ProtectedHeader>(jsonProtHeader);

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Incoming protected header: {JsonProtHeader}", jsonProtHeader);

        CheckProtectedHeader(ProtectedHeader);

        // -----  Check time.
        // According to the standard difference in time is not acceptable if:
        //   when signature time is greater then check time, by more then 2 seconds
        //   when signature time is less then check time, by more then 60 seconds
        if (CheckSignatureTimeConstraint)
        {
            var signatureTime = ProtectedHeader.SignatureTime.ToLocalTime();
            if (signatureTime > checkTime && (signatureTime - checkTime).TotalMilliseconds >= 2000)
                throw new SignatureVerificationProblemException($"The signing time is greater then current time of more then 2 seconds");
            if (checkTime > signatureTime && (checkTime - signatureTime).TotalMilliseconds > 60000)
                throw new SignatureVerificationProblemException($"The signing time is less than the current time of more than 60 seconds");
        }

        // -----  Check body digest
        // Get algorithm name from digest header value
        var digest = HttpDigest.CreateDigest(httpData.Headers[HttpMessageData.DigestHeadertName]);
        // Calculate http body digest
        var digestString = digest.CalculateDigest(httpData.Body);
        // compare digests
        if (!string.Equals(digestString, httpData.Headers[HttpMessageData.DigestHeadertName], StringComparison.Ordinal))
        {
            throw new SignatureVerificationProblemException($"Digest mismatch");
        }

        // -----  Check Signature

        // Check certificate: get certificate(s) from protected header and try to build certificate chain
        CreateCertificatesChain(ProtectedHeader);

        // Check and compare signing certificate organization identifier to clint certificate
        if (httpData is HttpRequestData)
            CheckOrganizationIdentifier(ProtectedHeader, httpData);

        // Compose payload
        var payload = httpData.ComposeHeadersForSignature(ProtectedHeader.DataToBeSigned.Parameters);

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("VerifySignature payload: {Payload}", payload);

        // Verify signature
        if (!VerifySignature(encodedProtectedHeader, payload, encodedSignature))
        {
            throw new SignatureVerificationProblemException("Can't verify signature");
        }

        IsSignatureVerified = true;

        return IsSignatureVerified;
    }

    private bool VerifySignature(string encodedProtHeader, string encodedPayload, string encodedSignature)
    {
        var verifier = SupportedAlgorithms.CreateVerifier(_signerCertificate, ProtectedHeader.AlgorithmName);
        return verifier.VerifySignature(encodedProtHeader, encodedPayload, encodedSignature);
    }

    private void CreateCertificatesChain(ProtectedHeader protHeader)
    {
        // According to https://tools.ietf.org/html/rfc7515#section-4.1.6, the first certificate
        // in the array should be a signer certificate

        _signerCertificate = protHeader.DecodeCertificate(protHeader.EncodedCertificates[0]);

        using var chain = new X509Chain();

        chain.ChainPolicy.RevocationMode = CertificateValidationFlags.RevocationMode;
        chain.ChainPolicy.RevocationFlag = CertificateValidationFlags.RevocationFlag;
        chain.ChainPolicy.VerificationFlags = CertificateValidationFlags.VerificationFlags;

        if (protHeader.EncodedCertificates.Count > 1)
        {
            foreach (var cert in protHeader.EncodedCertificates)
            {
                _ = chain.ChainPolicy.ExtraStore.Add(protHeader.DecodeCertificate(cert));
            }
        }

        _ = chain.Build(_signerCertificate);

        foreach (var status in chain.ChainStatus)
        {
            if (status.Status != X509ChainStatusFlags.NoError)
            {
                throw new CertificateValidationException(chain.ChainStatus, "Chain build error");
            }
        }
    }

    private static void CheckProtectedHeader(ProtectedHeader protHeader)
    {
        if (protHeader.EncodedCertificates.Count < 1)
            throw new SignatureVerificationProblemException("No certificates in 'x5c' field");

        // TODO: Perform more validations
    }

    private static void CheckMandatoryHeaders(T data)
    {
        if (!data.Headers.ContainsKey(HttpMessageData.SignatureHeaderName))
            throw new HeaderMissingException($"Mandatory header '{HttpMessageData.SignatureHeaderName}' is missing");

        data.CheckMandatoryHeaders();
    }

    private void CheckOrganizationIdentifier(ProtectedHeader protHeader, T data)
    {
        using var cert = protHeader.DecodeCertificate(protHeader.EncodedCertificates[0]);

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Incoming certificate: {Cert}", cert);

        var subjects = cert.Subject.Split(',');

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Incoming subjects: {Subject}", cert.Subject);

        string oidString = null;

        foreach (var oidSubjectName in HttpMessageData.OidSubjectNames)
        {
            oidString = subjects.FirstOrDefault(x => x.Contains(oidSubjectName));

            if (oidString != null)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Signing Certificate Organization identifier {OidSubjectName}: {OidString}", oidSubjectName, oidString);

                break;
            }
        }

        if (oidString == null)
            throw new CertificateValidationException("The organization identifier is missing in signing certificate");

        var oid = oidString.Split('=');
        if (oid.Length != 2)
            throw new CertificateValidationException("Invalid Organization identifier");

        if (!data.Headers.TryGetValue(HttpMessageData.OrganizationIdentifier, out string oidFromHeader))
            throw new HeaderMissingException($"Header '{HttpMessageData.OrganizationIdentifier}' is missing");

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Client Certificate Organization identifier: {OidFromHeader}", oidFromHeader);

        if (string.IsNullOrEmpty(oidFromHeader))
            throw new HeaderMissingException("The organization identifier headers from client's certificate is missing");

        if (!string.Equals(oid[1], oidFromHeader, StringComparison.Ordinal))
            throw new CertificateValidationException("The organization identifiers in signing certificate and client certificate does not match each other");
    }
}
