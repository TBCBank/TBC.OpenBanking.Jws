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
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

/// <summary>
/// Container for incoming or outgoing HTTP data.
/// </summary>
public abstract class HttpMessageData
{
    protected readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    public enum HeaderNecessity
    {
        Mandatory,
        IfExists,
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
    internal const char HeaderTerminatorInPayload = '\n';

    public const string DigestHeadertName = "digest";
    public const string SignatureHeaderName = "x-jws-signature";

    internal const string PsuPrefix = "psu-";

    internal const string SignatureSeparator = "..";
    internal readonly static string[] SignatureSplitter = { SignatureSeparator };

    internal readonly static byte[] EmptyBody = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the HTTP body.
    /// </summary>
    public byte[] Body { get; set; } = EmptyBody;

    /// <summary>
    /// Gets collection of HTTP headers.
    /// </summary>
    public IDictionary<string, string> Headers
    {
        get => _headers;
    }

    public void AddHeader(string name, string value)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (value == null) throw new ArgumentNullException(nameof(value));

        if (!Headers.ContainsKey(name))
        {
            Headers.Add(name, value);
        }
    }

    /// <summary>
    /// A utility method that appends <paramref name="httpHeaders"/> to the <see cref="Headers"/>
    /// </summary>
    /// <param name="httpHeaders">A collection of HTTP request headers</param>
    /// <param name="acceptMultivalue">If true, then multivalue headers are accepted and values will be concatenated to one string.
    /// If false, then multivalue headers are not acceptable and correspondent exeption will be thrown</param>
    public void AppendHeaders(HttpHeaders httpHeaders, bool acceptMultivalue = false)
    {
        if (httpHeaders == null) throw new ArgumentNullException(nameof(httpHeaders));

        var sb = new StringBuilder();
        foreach (var header in httpHeaders)
        {
            string headerValue;
            if (!Headers.ContainsKey(header.Key))
            {
                if (header.Value.Skip(1).Any())
                {
                    if (!acceptMultivalue)
                        throw new ArgumentOutOfRangeException(nameof(httpHeaders), $"Header {header.Key} contains multiple values");

                    sb.Clear();
                    foreach (var value in header.Value)
                    {
                        if (sb.Length != 0)
                            sb.Append(", ");
                        sb.Append(value);
                    }

                    headerValue = sb.ToString();
                }
                else
                    headerValue = header.Value.First();

                Headers.Add(header.Key, headerValue);
            }
        }
    }

    /// <remarks>
    /// <see href="https://tools.ietf.org/html/draft-cavage-http-signatures-10#section-2.3"/>
    ///
    /// 1.  If the header field name is `(request-target)` then generate the
    /// header field value by concatenating the lowercased :method, an
    /// ASCII space, and the :path pseudo-headers(as specified in
    /// HTTP/2, Section 8.1.2.3 [7]).  Note: For the avoidance of doubt,
    /// lowercasing only applies to the :method pseudo-header and not to
    /// the :path pseudo-header.
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
    ///
    /// 3.  If value is not the last value then append an ASCII newline `\n`.
    /// </remarks>
    public abstract string ComposeHeadersForSignature(IList<string> headers, IDictionary<string, string> additionalHeaders = null);

    /// <summary>
    /// If there are any mandatory/necessary headers, then it will check if all of them are present in Headers collection or not.
    /// On failure throws exception <see cref="Exceptions.HeaderMissingException"/>.
    /// </summary>
    public abstract void CheckMandatoryHeaders();

    public abstract List<string> GetHeaderNamesForSignature();
}
