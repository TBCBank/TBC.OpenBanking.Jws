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
    public class JwsConstants
    {
        public enum HeadersNecessity
        {
            Mandatory,
            IfExists
        }

        /// <summary>
        /// <see href="https://tools.ietf.org/html/draft-cavage-http-signatures-10#section-2.3"/>
        ///
        /// 2.  Create the header field string by concatenating the lowercased
        /// header field name followed with an ASCII colon `:`, an ASCII
        /// space ` `, and the header field value.Leading and trailing
        /// optional whitespace(OWS) in the header field value MUST be
        /// omitted(as specified in RFC7230[RFC7230], Section 3.2.4 [8]).
        /// If there are multiple instances of the same header field, all
        /// header field values associated with the header field MUST be
        /// concatenated, separated by a ASCII comma and an ASCII space `, `,
        /// and used in the order in which they will appear in the
        /// transmitted HTTP message.Any other modification to the header
        /// field value MUST NOT be made.
        /// </summary>
        internal const string HeaderNameValueSeparator = ": ";

        /// <summary>
        /// <see href="https://tools.ietf.org/html/draft-cavage-http-signatures-10#section-2.3"/>
        ///
        /// 3.  If value is not the last value then append an ASCII newline `\n`.
        /// </summary>
        internal const string HeaderTerminatorInPayload = "\n";

        internal const string SignatureSeparator = "..";
        internal readonly static string[] SignatureSplitter = { SignatureSeparator };

        internal const string PsuPrefix = "psu-";

        public const string RequestIDHeaderName = "x-request-id";
        public const string DigestHeadertName = "digest";
        public const string SignatureHeaderName = "x-jws-signature";
    }
}
