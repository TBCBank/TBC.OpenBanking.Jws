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
/// <para>
///     Table is based on <see href="https://tools.ietf.org/html/rfc7518#section-3.1"/>
/// </para>
/// <code>
///     +--------------+-------------------------------+--------------------+
///     | "alg" Param  | Digital Signature or MAC      | Implementation     |
///     | Value        | Algorithm                     | Requirements       |
///     +--------------+-------------------------------+--------------------+
///     | ES256        | ECDSA using P-256 and SHA-256 | Recommended+       |
///     | ES384        | ECDSA using P-384 and SHA-384 | Optional           |
///     | ES512        | ECDSA using P-521 and SHA-512 | Optional           |
///     +--------------+-------------------------------+--------------------+
/// </code>
/// </summary>
public class AlgorithmEcdsa : Algorithm
{
    private ECDsa ecdPrivate;
    private ECDsa ecdPublic;
    private int hashSize;
    private HashAlgorithmName hashName;
    private string algorithName;

    /// <inheritdoc/>
    public override string Name => algorithName;

    /// <inheritdoc/>
    public override HashAlgorithmName HashAlgorithmName => hashName;

    public AlgorithmEcdsa(X509Certificate2 cert, HashAlgorithmName hashName)
    {
        if (cert == null) throw new ArgumentNullException(nameof(cert));
        if (!cert.HasPrivateKey) throw new ArgumentException("Certificate should contain private key", nameof(cert));

        var privateKey = cert.GetECDsaPrivateKey();
        var publicKey = cert.GetECDsaPublicKey();

        Init(privateKey, publicKey, hashName);
    }

    public AlgorithmEcdsa(int keySize, HashAlgorithmName hashName)
    {
        var e = ECDsa.Create();
        e.KeySize = keySize;

        Init(e, e, hashName);
    }

    public AlgorithmEcdsa(ECDsa e, HashAlgorithmName hashName)
    {
        // TODO: The right way is to check if ECDsa contains private and public and set rsaPrivate and rsaPublic accordingly
        Init(e, e, hashName);
    }

    public AlgorithmEcdsa(ECParameters parameters, HashAlgorithmName hashName)
    {
        var e = ECDsa.Create();
        e.ImportParameters(parameters);

        // TODO: The right way is to check if ECDsa contains private and public and set rsaPrivate and rsaPublic accordingly
        Init(e, e, hashName);
    }

    ///// <summary>
    ///// ECDsa Cryptographic service provider.
    ///// </summary>
    //public ECDsa AsymmetricAlgorithm => ecdPrivate;

    /// <summary>
    /// Signs data.
    /// </summary>
    /// <param name="headerEncoded">Encoded properties to include in the header.</param>
    /// <param name="payloadEncoded">Encoded properties to include in the payload.</param>
    /// <returns>Signature encoded as Base64Url string</returns>
    public override string Sign(string headerEncoded, string payloadEncoded)
    {
        if (ecdPrivate == null) throw new CryptographicException("Private key is not set");

        byte[] data = Encoding.ASCII.GetBytes(headerEncoded + "." + payloadEncoded);
        byte[] signature = SignData(data);

        return signature.EncodeBase64Url();
    }

    /// <inheritdoc/>
    public override byte[] SignData(byte[] data)
    {
        if (ecdPrivate == null) throw new CryptographicException("Private key is not set");

        byte[] signature;
        lock (ecdPrivate)
        {
            signature = ecdPrivate.SignData(data, hashName);
        }

        return signature;
    }

    /// <summary>
    /// Checks if a signature is valid.
    /// </summary>
    /// <param name="headerEncoded">Encoded properties to include in the header.</param>
    /// <param name="payloadEncoded">Encoded properties to include in the payload.</param>
    /// <param name="signatureEncoded">Encoded signature.</param>
    /// <returns>If the signature is valid.</returns>
    public override bool VerifySignature(string headerEncoded, string payloadEncoded, string signatureEncoded)
    {
        if (ecdPublic == null) throw new CryptographicException("Public key is not set");

        byte[] data = Encoding.ASCII.GetBytes(headerEncoded + "." + payloadEncoded);
        return ecdPublic.VerifyData(data, signatureEncoded.DecodeBase64Url(), hashName);
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        ecdPrivate?.Dispose();

        if (ecdPublic != null && ecdPublic != ecdPrivate)
        {
            ecdPublic.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public override IDictionary<string, string> GetJwk(bool includePrivate)
    {
        throw new NotImplementedException();

        /*
        ECParameters parameters = ecdPrivate.ExportParameters(includePrivate);

        var dic = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "kty", "EC" },
            { "crv", "P-" + hashSize.ToString(CultureInfo.InvariantCulture) },
            { "x", Base64Url.Encode(parameters.Q.X)},
            { "y", Base64Url.Encode(parameters.Q.Y)},
        };

        if (includePrivate)
        {
            dic.Add("d", Base64Url.Encode(parameters.D));
        }

        return dic;
        */
    }

    private void Init(ECDsa privateKey, ECDsa publickey, HashAlgorithmName hashName)
    {
        ecdPrivate = privateKey;
        ecdPublic = publickey;
        hashSize = GetHashSize(hashName);
        this.hashName = hashName;
        algorithName = CreateAlgorithmName(hashSize);
    }

    private int GetHashSize(HashAlgorithmName hashName)
    {
        if (hashName == HashAlgorithmName.SHA256)
            return 256;
        if (hashName == HashAlgorithmName.SHA384)
            return 384;
        if (hashName == HashAlgorithmName.SHA512)
            return 512;

        throw new CryptographicException($"Not suitable hash algorithm {hashName.Name}");
    }

    private string CreateAlgorithmName(int hashSize)
    {
        return "ES" + hashSize.ToString(CultureInfo.InvariantCulture);
    }
}

// Table from https://tools.ietf.org/search/rfc4492#appendix-A
//------------------------------------------
//			Curve names chosen by
//		different standards organizations
//------------+---------------+-------------
//SECG        |  ANSI X9.62   |  NIST
//------------+---------------+-------------
//sect163k1   |               |   NIST K-163
//sect163r1   |               |
//sect163r2   |               |   NIST B-163
//sect193r1   |               |
//sect193r2   |               |
//sect233k1   |               |   NIST K-233
//sect233r1   |               |   NIST B-233
//sect239k1   |               |
//sect283k1   |               |   NIST K-283
//sect283r1   |               |   NIST B-283
//sect409k1   |               |   NIST K-409
//sect409r1   |               |   NIST B-409
//sect571k1   |               |   NIST K-571
//sect571r1   |               |   NIST B-571
//secp160k1   |               |
//secp160r1   |               |
//secp160r2   |               |
//secp192k1   |               |
//secp192r1   |  prime192v1   |   NIST P-192
//secp224k1   |               |
//secp224r1   |               |   NIST P-224
//secp256k1   |               |
//secp256r1   |  prime256v1   |   NIST P-256
//secp384r1   |               |   NIST P-384
//secp521r1   |               |   NIST P-521
//------------+---------------+-------------
