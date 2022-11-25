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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class HttpSigner<T> where T : HttpMessageData
{
    private readonly ILogger<HttpSigner<T>> _logger;
    protected readonly Dictionary<string, string> _headersToSign = new(StringComparer.OrdinalIgnoreCase);

    public HttpSigner(ILogger<HttpSigner<T>> logger)
    {
        _logger = logger ?? NullLoggerFactory.Instance.CreateLogger<HttpSigner<T>>();
        DigestHashAlgorithmName = null;

        Reset();
    }

    private void Reset()
    {
        ProtectedHeader = null;
        IsSignatureCreated = false;
        SignatureHeaderValue = null;
        DigestHeaderValue = null;
    }

    public ISigner Signer { get; set; }

    public X509Certificate2 SignerCertificate { get; set; }

    public X509Certificate2Collection SignerCertificateChain { get; set; }

    public HashAlgorithmName? DigestHashAlgorithmName { get; set; }

    public ProtectedHeader ProtectedHeader { get; private set; }

    /// <summary>
    /// True if the last call to CreateSignature finished well
    /// </summary>
    public bool IsSignatureCreated { get; private set; }

    /// <summary>
    /// Signature header value
    /// </summary>
    public string SignatureHeaderValue { get; private set; }

    /// <summary>
    /// Request body digest calculated during last call of CreateSignature
    /// </summary>
    public string DigestHeaderValue { get; private set; }

    /// <summary>
    /// Sign data from <paramref name="httpData"/> using crypto stuff from object constracting.
    /// As a result SignatureHeaderValue and DigestHeaderValue are calculated
    /// You can call this method several times. Each time new signature and values will be created
    /// </summary>
    /// <param name="httpData">Request data</param>
    /// <returns>true if signature created without a problem</returns>
    public bool CreateSignature(T httpData)
    {
        if (httpData == null) throw new ArgumentNullException(nameof(httpData));
        if (Signer == null) throw new InvalidOperationException($"Property not set '{nameof(Sign)}'");
        if (SignerCertificate == null) throw new InvalidOperationException($"Property not set '{nameof(SignerCertificate)}'");
        if (SignerCertificateChain == null) throw new InvalidOperationException($"Property not set '{nameof(SignerCertificateChain)}'");

        Reset();

        // Calculate body digest
        var digest = new HttpDigest(DigestHashAlgorithmName ?? Signer.HashAlgorithmName);
        var digestValue = digest.CalculateDigest(httpData.Body);

        var additionalHeaderValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [JwsConstants.DigestHeadertName] = digestValue,
        };

        var headersList = httpData.GetHeaderNamesForSignature();

        var protHeader = ComposeProtectedHeader(headersList);
        var jsonProtectedHeader = Helper.SerializeToJson(protHeader);
        var encodedProtHeader = UTF8EncodingSealed.Instance.GetBytes(jsonProtectedHeader).EncodeBase64Url();

        var payload = httpData.ComposeHeadersForSignature(protHeader.DataToBeSigned.Parameters, additionalHeaderValues);

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("CreateSignature payload: {Payload}", payload);

        var encodedSignature = Sign(encodedProtHeader, payload);

        // Populate properties
        ProtectedHeader = protHeader;
        DigestHeaderValue = digestValue;
        SignatureHeaderValue = $"{encodedProtHeader}{JwsConstants.SignatureSeparator}{encodedSignature}";

        IsSignatureCreated = true;

        return IsSignatureCreated;
    }

    private string Sign(string encodedProtHeader, string payload)
    {
        byte[] data = Encoding.ASCII.GetBytes(encodedProtHeader + "." + payload);
        byte[] signature = Signer.SignData(data);

        return signature.EncodeBase64Url();
    }

    private ProtectedHeader ComposeProtectedHeader(List<string> headerNames)
    {
        ProtectedHeader protectedHeader = new();

        foreach (var hn in headerNames)
        {
            protectedHeader.DataToBeSigned.AddParameter(hn);
        }

        protectedHeader.AlgorithmName = Signer.Name;
        protectedHeader.SetEncodedCertificates(SignerCertificate, SignerCertificateChain);

        return protectedHeader;
    }
}
