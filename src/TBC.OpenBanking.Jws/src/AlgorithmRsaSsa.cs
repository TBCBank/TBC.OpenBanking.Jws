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
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

/// <summary>
/// Table is based on <see href="https://tools.ietf.org/html/rfc7518#section-3.1"/>
/// <code>
/// +--------------+-------------------------------+--------------------+
/// | "alg" Param  | Digital Signature or MAC      | Implementation     |
/// | Value        | Algorithm                     | Requirements       |
/// +--------------+-------------------------------+--------------------+
/// | RS256        | RSASSA-PKCS1-v1_5 using       | Recommended        |
/// |              | SHA-256                       |                    |
/// | RS384        | RSASSA-PKCS1-v1_5 using       | Optional           |
/// |              | SHA-384                       |                    |
/// | RS512        | RSASSA-PKCS1-v1_5 using       | Optional           |
/// |              | SHA-512                       |                    |
/// | PS256        | RSASSA-PSS using SHA-256 and  | Optional           |
/// |              | MGF1 with SHA-256             |                    |
/// | PS384        | RSASSA-PSS using SHA-384 and  | Optional           |
/// |              | MGF1 with SHA-384             |                    |
/// | PS512        | RSASSA-PSS using SHA-512 and  | Optional           |
/// |              | MGF1 with SHA-512             |                    |
/// +--------------+-------------------------------+--------------------+
/// </code>
/// https://tools.ietf.org/html/rfc3447#section-8.2
/// </summary>
public class AlgorithmRsaSsa : Algorithm
{
    private RSA rsaPrivate;
    private RSA rsaPublic;
    private HashAlgorithmName hashName;
    private RSASignaturePadding padding;
    private string algorithName;

    /// <inheritdoc/>
    public override string Name => algorithName;

    /// <inheritdoc/>
    public override HashAlgorithmName HashAlgorithmName => hashName;

    public AlgorithmRsaSsa(int keySize, HashAlgorithmName hashName, RSASignaturePadding padding)
    {
        var r = RSA.Create();
        r.KeySize = keySize;

        Init(r, r, hashName, padding);
    }

    public AlgorithmRsaSsa(X509Certificate2 cert, HashAlgorithmName hashName, RSASignaturePadding padding)
    {
        if (cert == null) throw new ArgumentNullException(nameof(cert));
        if (padding == null) throw new ArgumentNullException(nameof(padding));

        var privateKey = cert.GetRSAPrivateKey();
        var publicKey = cert.GetRSAPublicKey();

        Init(privateKey, publicKey, hashName, padding);
    }

    public AlgorithmRsaSsa(RSA r, HashAlgorithmName hashName, RSASignaturePadding padding)
    {
        // TODO: The right way is to check if RSA contains private and public and set rsaPrivate and rsaPublic accordingly
        Init(r, r, hashName, padding);
    }

    public AlgorithmRsaSsa(RSAParameters Parameters, HashAlgorithmName hashName, RSASignaturePadding padding)
    {
        var r = RSA.Create();
        r.ImportParameters(Parameters);

        // TODO: The right way is to check if parameters contains private and public and set rsaPrivate and rsaPublic accordingly
        Init(r, r, hashName, padding);
    }

    ///// <summary>
    ///// RSA Cryptographic service provider.
    ///// </summary>
    //public RSA AsymmetricAlgorithm => rsaPrivate;

    /// <summary>
    /// Signs data.
    /// </summary>
    /// <param name="headerEncoded">Encoded properties to include in the header.</param>
    /// <param name="payloadEncoded">Encoded properties to include in the payload.</param>
    /// <returns>Signature encoded as Base64Url string</returns>
    public override string Sign(string headerEncoded, string payloadEncoded)
    {
        if (rsaPrivate == null) throw new CryptographicException("Private key is not set");

        byte[] data = Encoding.ASCII.GetBytes(headerEncoded + "." + payloadEncoded);
        byte[] signature = SignData(data);

        return signature.EncodeBase64Url();
    }

    /// <inheritdoc/>
    public override byte[] SignData(byte[] data)
    {
        if (rsaPrivate == null) throw new CryptographicException("Private key is not set");

        byte[] signature;
        lock (rsaPrivate)
        {
            signature = rsaPrivate.SignData(data, hashName, padding);
        }

        return signature;
    }

    /// <inheritdoc/>
    public override bool VerifySignature(string headerEncoded, string payloadEncoded, string signatureEncoded)
    {
        if (rsaPublic == null) throw new CryptographicException("Public key is not set");

        byte[] TokenBin = Encoding.ASCII.GetBytes(headerEncoded + "." + payloadEncoded);
        return rsaPublic.VerifyData(TokenBin, signatureEncoded.DecodeBase64Url(), hashName, padding);
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        rsaPrivate?.Dispose();

        if (rsaPublic != null && rsaPublic != rsaPrivate)
        {
            rsaPublic.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public override IDictionary<string, string> GetJwk(bool includePrivate)
    {
        throw new NotImplementedException();

        /*
        RSAParameters parameters = rsaPrivate.ExportParameters(includePrivate);

        var dic = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "kty", "RSA" },
            { "n", parameters.Modulus.EncodeBase64Url() },
            { "e", parameters.Exponent.EncodeBase64Url() },
        };

        if (includePrivate)
        {
            dic.Add("d", Base64Url.Encode(parameters.D));
            dic.Add("p", Base64Url.Encode(parameters.P));
            dic.Add("q", Base64Url.Encode(parameters.Q));
            dic.Add("dp", Base64Url.Encode(parameters.DP));
            dic.Add("dq", Base64Url.Encode(parameters.DQ));
            dic.Add("qi", Base64Url.Encode(parameters.InverseQ));
        }

        return dic;
        */
    }

    private void Init(RSA privateKey, RSA publicKey, HashAlgorithmName hashName, RSASignaturePadding padding)
    {
        this.rsaPrivate = privateKey;
        this.rsaPublic = publicKey;
        this.hashName = hashName;
        this.padding = padding;
        this.algorithName = CreateAlgorithmName(padding, hashName);
    }

    private string CreateAlgorithmName(RSASignaturePadding padding, HashAlgorithmName hashName)
    {
        int hashSize;
        if (hashName == HashAlgorithmName.SHA256) hashSize = 256;
        else if (hashName == HashAlgorithmName.SHA384) hashSize = 386;
        else if (hashName == HashAlgorithmName.SHA512) hashSize = 512;
        else
        {
            throw new CryptographicException($"Unsuitable hash algorithm '{hashName.Name}'.");
        }

        return ((padding == RSASignaturePadding.Pkcs1) ? "RS" : "PS") + hashSize.ToString(CultureInfo.InvariantCulture);
    }
}
