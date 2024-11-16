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
using TBC.OpenBanking.Jws.Exceptions;

/// <summary>
/// Container for incoming or outgoing HTTP request data.
/// </summary>
public class HttpResponseData : HttpMessageData
{
    /// <summary>
    /// <c>(response-status)</c> represents status code
    /// </summary>
    public const string ResponseStatusHeaderName = "(response-status)";

    public static readonly IReadOnlyList<(string Name, HeaderNecessity Necessity)> NecessaryHeaders =
        new List<(string, HeaderNecessity)>(5)
        {
            (ResponseStatusHeaderName, HeaderNecessity.Mandatory),
            ("x-request-id", HeaderNecessity.Mandatory),
            ("content-type", HeaderNecessity.IfExists),
            ("content-length", HeaderNecessity.IfExists),
            (DigestHeadertName, HeaderNecessity.Mandatory),
        };

    public string StatusCode { get; set; }

    public override void CheckMandatoryHeaders()
    {
        foreach (var (name, necessity) in NecessaryHeaders)
        {
            if (string.Equals(name, ResponseStatusHeaderName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (necessity == HeaderNecessity.Mandatory && !Headers.ContainsKey(name))
                throw new HeaderMissingException($"Mandatory header '{name}' is missing");
        }
    }

    public override string ComposeHeadersForSignature(IList<string> headers, IDictionary<string, string> additionalHeaders = null)
    {
        _ = headers ?? throw new ArgumentNullException(nameof(headers));

        using var sb = new ValueStringBuilder(initialCapacity: 512);
        foreach (var hn in headers)
        {
            if (sb.Length > 0)
                sb.Append(HttpMessageData.HeaderTerminatorInPayload);

            if (string.Equals(hn, ResponseStatusHeaderName, StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(hn);
                sb.Append(HttpMessageData.HeaderNameValueSeparator);
                sb.Append(StatusCode);
            }
            else
            {
                if (!Headers.TryGetValue(hn, out string headerValue)
                    && (additionalHeaders == null || !additionalHeaders.TryGetValue(hn, out headerValue)))
                {
                    throw new HeaderMissingException($"Can't find header '{hn}'");
                }

                sb.Append(hn);
                sb.Append(HeaderNameValueSeparator);
                sb.Append(headerValue);
            }
        }

        return sb.ToString();
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Intentional")]
    public override List<string> GetHeaderNamesForSignature()
    {
        var headersList = new List<string>(NecessaryHeaders.Count);

        // Check HTTP headers collection
        foreach (var (Name, Necessity) in NecessaryHeaders)
        {
            if (string.Equals(Name, ResponseStatusHeaderName, StringComparison.OrdinalIgnoreCase))
            {
                headersList.Add(ResponseStatusHeaderName);
            }
            else if (string.Equals(Name, DigestHeadertName, StringComparison.OrdinalIgnoreCase))
            {
                headersList.Add(DigestHeadertName);
            }
            else
            {
                if (Headers.TryGetValue(Name, out string headerValue))
                {
                    headersList.Add(Name.ToLowerInvariant());
                }
                else
                {
                    if (Necessity == HeaderNecessity.Mandatory)
                    {
                        throw new HeaderMissingException($"Mandatory header missing {Name}");
                    }
                }
            }
        }

        return headersList;
    }
}
