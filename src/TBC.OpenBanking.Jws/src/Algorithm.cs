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

public abstract class Algorithm : IDisposable, ISigner
{
    /// <summary>
    /// Short name for algorithm. Corresponds to JWS &quot;alg&quot; header parameter.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Cryptographic hash algorithm name.
    /// </summary>
    public abstract HashAlgorithmName HashAlgorithmName { get; }

    /// <summary>
    /// Signs data.
    /// </summary>
    /// <param name="headers">Properties to include in the header.</param>
    /// <param name="payload">Properties to include in the payload.</param>
    /// <param name="encodedHeader">Resulting encoded header string.</param>
    /// <param name="encodedPayload">Resulting encoded payload string.</param>
    /// <param name="encodedSignature">Generated signature encoded as Base64Url string.</param>
    public virtual void Sign(IDictionary<string, object> headers, IDictionary<string, object> payload,
        out string encodedHeader, out string encodedPayload, out string encodedSignature)
    {
        var headerJson = Helper.SerializeToJson(headers);
        encodedHeader = UTF8EncodingSealed.Instance.GetBytes(headerJson).EncodeBase64Url();

        string PayloadJson = payload is null ? string.Empty : Helper.SerializeToJson(payload);
        encodedPayload = UTF8EncodingSealed.Instance.GetBytes(PayloadJson).EncodeBase64Url();

        encodedSignature = this.Sign(encodedHeader, encodedPayload);
    }

    /// <summary>
    /// Signs data.
    /// </summary>
    /// <param name="headerEncoded">Encoded properties to include in the header.</param>
    /// <param name="payloadEncoded">Encoded properties to include in the payload.</param>
    /// <returns>Signature encoded as Base64Url string</returns>
    public abstract string Sign(string headerEncoded, string payloadEncoded);

    /// <summary>
    /// Checks if a signature is valid.
    /// </summary>
    /// <param name="headerEncoded">Encoded properties to include in the header.</param>
    /// <param name="payloadEncoded">Encoded properties to include in the payload.</param>
    /// <param name="signatureEncoded">Encoded signature.</param>
    /// <returns>If the signature is valid.</returns>
    public abstract bool VerifySignature(string headerEncoded, string payloadEncoded, string signatureEncoded);

    /// <inheritdoc />
    public abstract void Dispose();

    /// <summary>
    /// Returns JSON Web Key contained in a dictionary.
    /// </summary>
    /// <param name="includePrivate">If private parameters are to be included.</param>
    /// <returns>JWK for current object.</returns>
    public abstract IDictionary<string, string> GetJwk(bool includePrivate);

    /// <summary>
    /// Signs data.
    /// </summary>
    /// <param name="data">Data to be signed.</param>
    /// <returns>Signature</returns>
    public abstract byte[] SignData(byte[] data);
}
