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

namespace TBC.OpenBanking.Jws
{
    /// <summary>
    /// Implementation of <see href="https://datatracker.ietf.org/doc/html/rfc4648#section-5"/>.
    /// </summary>
    public static class Base64Url
    {
        /// <summary>
        /// Converts Base64URL string to its binary representation
        /// </summary>
        /// <param name="base64Url">Base64URL-encoded string</param>
        /// <returns>Binary representation</returns>
        public static byte[] DecodeBase64Url(this string base64Url) =>
            WebEncoders.Base64UrlDecode(base64Url);

        /// <summary>
        /// Convert binary block to Base64URL encoding
        /// </summary>
        /// <param name="data">input byte array</param>
        /// <returns>Base64URL encoded string</returns>
        public static string EncodeBase64Url(this byte[] data) =>
            WebEncoders.Base64UrlEncode(data);
    }
}
