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

internal class HttpDigest
{
    private readonly HashAlgorithm hashAlgorithm;
    private readonly string digestPrefix;

    internal static readonly List<(HashAlgorithmName AlgorithName, string Prefix)> supportedAlgorithms = new(3)
    {
        (HashAlgorithmName.SHA256, "SHA-256"),
        (HashAlgorithmName.SHA384, "SHA-384"),
        (HashAlgorithmName.SHA512, "SHA-512"),
    };

    internal HashAlgorithm HashAlgorithm => this.hashAlgorithm;

    internal HttpDigest(HashAlgorithmName hashAlgorithmName)
    {
        digestPrefix = GetDigestHeader(hashAlgorithmName);

        // HashAlgorithm.Create(name) is obsolete
        if (hashAlgorithmName == HashAlgorithmName.SHA256)
        {
            hashAlgorithm = SHA256.Create();
        }
        else if (hashAlgorithmName == HashAlgorithmName.SHA384)
        {
            hashAlgorithm = SHA384.Create();
        }
        else if (hashAlgorithmName == HashAlgorithmName.SHA512)
        {
            hashAlgorithm = SHA512.Create();
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(hashAlgorithmName), "Unsupported hash algorithm");
        }
    }

    internal static HttpDigest CreateDigest(string digestString)
    {
        // Example:
        // SHA-256=+xeh7JAayYPh8K13UnQCBBcniZzsyat+KDiuy8aZYdI=

        int dividerIndex = digestString.IndexOf('=');
        if (dividerIndex == -1)
            throw new ArgumentOutOfRangeException(nameof(digestString), "Bad format of digest string. Can't find algorithm prefix");

        var algName = digestString.Substring(0, dividerIndex).Trim();
        var element = supportedAlgorithms.Find(x => string.Equals(x.Prefix, algName, StringComparison.Ordinal));
        if (element == default)
            throw new ArgumentOutOfRangeException(nameof(digestString), "Unsupported hash algorithm");

        return new HttpDigest(element.AlgorithName);
    }

    private string GetDigestHeader(HashAlgorithmName hashAlgorithmName)
    {
        var element = supportedAlgorithms.Find(x => x.AlgorithName == hashAlgorithmName);
        if (element == default)
            throw new ArgumentOutOfRangeException(nameof(hashAlgorithmName), "Unsupported hash algorithm");

        return element.Prefix;
    }

    internal string CalculateDigest(string body)
    {
        return CalculateDigest(UTF8EncodingSealed.Instance.GetBytes(body));
    }

    internal string CalculateDigest(byte[] body)
    {
        if (body is null) throw new ArgumentNullException(nameof(body));

        byte[] hashValue = hashAlgorithm.ComputeHash(body);
        string hashEncoded = Convert.ToBase64String(hashValue);
        return $"{digestPrefix}={hashEncoded}";
    }

#if NETCOREAPP3_1_OR_GREATER
    internal string CalculateDigest(ReadOnlySpan<byte> body)
    {
        Span<byte> hash = stackalloc byte[hashAlgorithm.HashSize / 8];
        hashAlgorithm.TryComputeHash(body, hash, out int bytesWritten);
        Span<char> chars = stackalloc char[ToBase64_CalculateAndValidateOutputLength(bytesWritten)];
        Convert.TryToBase64Chars(hash, chars, out _, Base64FormattingOptions.None);
        return digestPrefix + "=" + new string(chars);
    }

    private static int ToBase64_CalculateAndValidateOutputLength(int inputLength)
    {
        int outlen = inputLength / 3 * 4;
        outlen += ((inputLength % 3) != 0) ? 4 : 0;
        return outlen;
    }
#endif
}
