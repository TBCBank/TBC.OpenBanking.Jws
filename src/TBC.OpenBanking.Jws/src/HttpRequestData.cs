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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using TBC.OpenBanking.Jws.Exceptions;

/// <summary>
/// Container for incoming or outgoing HTTP request data.
/// </summary>
public class HttpRequestData : HttpMessageData
{
    public const string RequestTargetHeaderName = "(request-target)";

    public readonly static IReadOnlyList<(string Name, HeaderNecessity Necessity)> NecessaryHeaders =
        new List<(string, HeaderNecessity)>
        {
            (RequestTargetHeaderName, HeaderNecessity.Mandatory),
            ("host", HeaderNecessity.Mandatory),
            ("x-request-id", HeaderNecessity.Mandatory),
            ("content-type", HeaderNecessity.IfExists),
            ("content-length", HeaderNecessity.IfExists),
            (DigestHeadertName, HeaderNecessity.Mandatory),
        };

    /// <summary>
    /// Gets or sets the HTTP request URL.
    /// </summary>
    public Uri Uri { get; set; }

    /// <summary>
    /// Gets or sets the HTTP request method.
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    /// Creates, so called, signature payload string, which consist header name and value pairs separated by '\n'.
    /// HttpMessageData.Headers and <paramref name="additionalHeaders"/> are used as a source for headers data.
    /// Special header name "(request-target)" is also acceptable and will be processed accordingly.
    /// </summary>
    /// <param name="headers">List of header names which should be added to payload.
    /// Special header name "(request-target)" is also acceptable and will be processed accordingly.</param>
    /// <param name="additionalHeaders">Additional header name-values</param>
    /// <returns></returns>
    public override string ComposeHeadersForSignature(IList<string> headers, IDictionary<string, string> additionalHeaders = null)
    {
        var sb = new StringBuilder();
        foreach (var hn in headers)
        {
            if (sb.Length > 0)
                sb.Append(HttpMessageData.HeaderTerminatorInPayload);

            if (string.Equals(hn, RequestTargetHeaderName, StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(hn)
                    .Append(HttpMessageData.HeaderNameValueSeparator)
                    .Append(Method.ToLowerInvariant())
                    .Append(' ')
                    .Append(Uri.PathAndQuery);
            }
            else
            {
                if (!Headers.TryGetValue(hn, out string headerValue)
                    && (additionalHeaders == null || !additionalHeaders.TryGetValue(hn, out headerValue)))
                {
                    throw new HeaderMissingException($"Can't find header '{hn}'");
                }

                sb.Append(hn)
                    .Append(HttpMessageData.HeaderNameValueSeparator)
                    .Append(headerValue);
            }
        }

        return sb.ToString();
    }

    // /// <summary>
    // /// Collect headers data for signing.
    // /// Headers are fetched from <paramref name="data"/>
    // /// a) by name from necessaryHeaderNames;
    // /// b) by matching prefix "psu-*"
    // /// b) special headers "(request-target)" from data.Uri and "digest" from with value <paramref name="digestValue"/>
    // /// </summary>
    // /// <param name="additionalHeaders">Values for additional (external) headers, for example, digest header</param>
    // /// <returns>Dictionary with values of headers</returns>
    //public IDictionary<string, string> CollectHeadersData(IDictionary<string, string> additionalHeaders)
    //{
    //    var headersToSign = new Dictionary<string, string>();

    //    // Check HTTP headers collection
    //    foreach (var nh in NecessaryHeaders)
    //    {
    //        if (nh.Name == RequestTargetHeaderName)
    //        {
    //            headersToSign.Add(RequestTargetHeaderName, $"{Method.ToLower()} {Uri.PathAndQuery}");
    //        }
    //        else if (nh.Name == DigestHeadertName)
    //        {
    //            if (additionalHeaders.TryGetValue(nh.Name, out string headerValue))
    //                headersToSign.Add(DigestHeadertName, headerValue);
    //            else
    //                throw new HeaderMissingException($"Mandatory header missing '{nh.Name}'");
    //        }
    //        else
    //        {
    //            if (Headers.TryGetValue(nh.Name, out string headerValue))
    //            {
    //                headersToSign.Add(nh.Name.ToLower(), headerValue);
    //            }
    //            else
    //            {
    //                if (nh.Necessity == HeaderNecessity.Mandatory)
    //                {
    //                    throw new HeaderMissingException($"Mandatory header missing '{nh.Name}'");
    //                }
    //            }
    //        }
    //    }

    //    // Add "psu-*" OpenBanking headers
    //    foreach (var kv in Headers)
    //    {
    //        var key = kv.Key.ToLower();
    //        if (key.IndexOf(PsuPrefix) == 0)
    //        {
    //            headersToSign.Add(key, kv.Value);
    //        }
    //    }

    //    return headersToSign;
    //}

    public override void CheckMandatoryHeaders()
    {
        foreach (var (name, necessity) in NecessaryHeaders)
        {
            if (string.Equals(name, RequestTargetHeaderName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (necessity == HeaderNecessity.Mandatory && !Headers.ContainsKey(name))
                throw new HeaderMissingException($"Mandatory header '{name}' is missing");
        }
    }

    public override List<string> GetHeaderNamesForSignature()
    {
        var headersList = new List<string>(NecessaryHeaders.Count);

        // Check HTTP headers collection
        foreach (var (name, necessity) in NecessaryHeaders)
        {
            if (string.Equals(name, RequestTargetHeaderName, StringComparison.OrdinalIgnoreCase))
            {
                headersList.Add(RequestTargetHeaderName);
            }
            else if (string.Equals(name, DigestHeadertName, StringComparison.OrdinalIgnoreCase))
            {
                headersList.Add(DigestHeadertName);
            }
            else
            {
                if (Headers.TryGetValue(name, out string headerValue))
                {
                    headersList.Add(name.ToLowerInvariant());
                }
                else
                {
                    if (necessity == HeaderNecessity.Mandatory)
                    {
                        throw new HeaderMissingException($"Mandatory header is missing: {name}");
                    }
                }
            }
        }

        // Add PSU* headers
        headersList.AddRange((from hn in Headers.Keys
                              where hn.IndexOf(PsuPrefix, StringComparison.OrdinalIgnoreCase) == 0
                              select hn).ToList());

        return headersList;
    }
}
