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

/// <example>
/// <para>
///     Table is based on <see href="https://tools.ietf.org/html/rfc7518#section-3.1"/>
/// </para>
/// <para>
/// <code>
///     +--------------+-------------------------------+--------------------+
///     | "alg" Param  | Digital Signature or MAC      | Implementation     |
///     | Value        | Algorithm                     | Requirements       |
///     +--------------+-------------------------------+--------------------+
///     | HS256        | HMAC using SHA-256            | Required           |
///     | HS384        | HMAC using SHA-384            | Optional           |
///     | HS512        | HMAC using SHA-512            | Optional           |
///     | RS256        | RSASSA-PKCS1-v1_5 using       | Recommended        |
///     |              | SHA-256                       |                    |
///     | RS384        | RSASSA-PKCS1-v1_5 using       | Optional           |
///     |              | SHA-384                       |                    |
///     | RS512        | RSASSA-PKCS1-v1_5 using       | Optional           |
///     |              | SHA-512                       |                    |
///     | ES256        | ECDSA using P-256 and SHA-256 | Recommended+       |
///     | ES384        | ECDSA using P-384 and SHA-384 | Optional           |
///     | ES512        | ECDSA using P-521 and SHA-512 | Optional           |
///     | PS256        | RSASSA-PSS using SHA-256 and  | Optional           |
///     |              | MGF1 with SHA-256             |                    |
///     | PS384        | RSASSA-PSS using SHA-384 and  | Optional           |
///     |              | MGF1 with SHA-384             |                    |
///     | PS512        | RSASSA-PSS using SHA-512 and  | Optional           |
///     |              | MGF1 with SHA-512             |                    |
///     | none         | No digital signature or MAC   | Optional           |
///     |              | performed                     |                    |
///     +--------------+-------------------------------+--------------------+
/// </code>
/// </para>
/// <para>
///     Algorithms HS* and none are'nt supported
/// </para>
/// </example>
public static class SupportedAlgorithms
{
    public const string RsaPKCS1Sha256 = "RS256";
    public const string RsaPKCS1Sha384 = "RS384";
    public const string RsaPKCS1Sha512 = "RS512";

    public const string RsaSsaPssSha256 = "PS256";
    public const string RsaSsaPssSha384 = "PS384";
    public const string RsaSsaPssSha512 = "PS512";

    public const string EcdsaSha256 = "ES256";
    public const string EcdsaSha384 = "ES384";
    public const string EcdsaSha512 = "ES512";

    private static readonly Dictionary<string, Func<X509Certificate2, Algorithm>> supportedAlgorithms = new(9, StringComparer.OrdinalIgnoreCase)
    {
        [RsaPKCS1Sha256] = cert => new AlgorithmRsaSsa(cert, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1),
        [RsaPKCS1Sha384] = cert => new AlgorithmRsaSsa(cert, HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1),
        [RsaPKCS1Sha512] = cert => new AlgorithmRsaSsa(cert, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1),

        [RsaSsaPssSha256] = cert => new AlgorithmRsaSsa(cert, HashAlgorithmName.SHA256, RSASignaturePadding.Pss),
        [RsaSsaPssSha384] = cert => new AlgorithmRsaSsa(cert, HashAlgorithmName.SHA384, RSASignaturePadding.Pss),
        [RsaSsaPssSha512] = cert => new AlgorithmRsaSsa(cert, HashAlgorithmName.SHA512, RSASignaturePadding.Pss),

        [EcdsaSha256] = cert => new AlgorithmEcdsa(cert, HashAlgorithmName.SHA256),
        [EcdsaSha384] = cert => new AlgorithmEcdsa(cert, HashAlgorithmName.SHA384),
        [EcdsaSha512] = cert => new AlgorithmEcdsa(cert, HashAlgorithmName.SHA512),
    };

    static public bool IsSupportedAlgorithm(string alg) => supportedAlgorithms.ContainsKey(alg);

    /// <summary>
    ///
    /// </summary>
    /// <param name="cert">Certificate with private key</param>
    /// <param name="alg"></param>
    /// <returns></returns>
    static public ISigner CreateSigner(X509Certificate2 cert, string alg)
    {
        if (!cert.HasPrivateKey)
            throw new ArgumentOutOfRangeException(nameof(cert), "Private key is missing");

        if (!supportedAlgorithms.TryGetValue(alg, out var creator))
        {
            // Error. Unsupported algorithm
            throw new ArgumentOutOfRangeException(nameof(alg), $"Unsupported algorithm '{alg}'");
        }

        return creator(cert);
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="cert">Certificate with public key</param>
    /// <param name="alg"></param>
    /// <returns></returns>
    static public Algorithm CreateVerifier(X509Certificate2 cert, string alg)
    {
        if (!supportedAlgorithms.TryGetValue(alg, out var creator))
        {
            // Error. Unsupported algorithm
            throw new ArgumentOutOfRangeException(nameof(alg), $"Unsupported algorithm '{alg}'");
        }

        return creator(cert);
    }
}
