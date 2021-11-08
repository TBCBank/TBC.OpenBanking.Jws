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

namespace SampleConsole
{
    using System;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using Microsoft.Extensions.Logging;
    using TBC.OpenBanking.Jws;
    using TBC.OpenBanking.Jws.Exceptions;

    class Program
    {
        static void Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("", LogLevel.Trace)
                    .AddConsole();
            });

            var publicKeyCertFile = @".\keyandcert.pfx";
            var privateKeyCertFile = @".\keyandcert.pfx";
            var password = "";


            X509Certificate2 publicKeyCert = new X509Certificate2(new X509Certificate2(File.ReadAllBytes(publicKeyCertFile), password).Export(X509ContentType.Cert));
            X509Certificate2 privateKeyCert = new X509Certificate2(File.ReadAllBytes(privateKeyCertFile), password);


            //"RS256"
            var algorithmName = SupportedAlgorithms.RsaPKCS1Sha256;

            // Request
            var httpRequestFileName = "HttpRequest-001.txt";
            var signedHttpRequestFileName = "SignedRequest.txt";

            SignHttpRequestSample(algorithmName, httpRequestFileName, signedHttpRequestFileName, publicKeyCert, privateKeyCert, loggerFactory);
            VerifyHttpRequestSignatureSample(signedHttpRequestFileName, loggerFactory);

            // Response
            var httpResponseFileName = "HttpResponse-001.txt";
            var signedHttpResponseFileName = "SignedResponse.txt";

            SignHttpResponseSample(algorithmName, httpResponseFileName, signedHttpResponseFileName, publicKeyCert, privateKeyCert, loggerFactory);
            VerifyHttpResponseSignatureSample(signedHttpResponseFileName, loggerFactory);

        }

        private static void SignHttpRequestSample(
            string algorithmName,
            string inFileName,
            string outFileName,
            X509Certificate2 publicKeyCert,
            X509Certificate2 privateKeyCert,
            ILoggerFactory loggerFactory)
        {
            try
            {
                // Get HttpRequestData from HTTP Request
                var httpData = ReadHttpRequestDataFromString(File.ReadAllText(inFileName));

                // Get certificate chain
                // If it is possible, better to cache chain, because chain creation is slow
                X509Certificate2Collection chainCertificates = GetCertificateChain(publicKeyCert);

                // Get ISigner
                var signer = SupportedAlgorithms.CreateSigner(privateKeyCert, algorithmName);

                // Create HttpSigner.
                // Need ISigner, certificate with signer's public key and
                // certificate chain in X509Certificate2Collection
                var reqSign = new HttpSigner<HttpRequestData>(loggerFactory.CreateLogger<HttpSigner<HttpRequestData>>())
                {
                    Signer = signer,
                    SignerCertificate = publicKeyCert,
                    SignerCertificateChain = chainCertificates
                };

                // Create signature
                reqSign.CreateSignature(httpData);

                if (reqSign.IsSignatureCreated)
                {
                    httpData.Headers.Add(HttpMessageData.DigestHeadertName, reqSign.DigestHeaderValue);
                    httpData.Headers.Add(HttpMessageData.SignatureHeaderName, reqSign.SignatureHeaderValue);

                    WriteHttpRequestDataToFile(httpData, outFileName);
                }
            }
            catch (Exception x)
            {
                Console.Error.WriteLine(x.ToString());
            }
        }

        private static void VerifyHttpRequestSignatureSample(string inFileName, ILoggerFactory loggerFactory)
        {
            try
            {
                // Get HttpRequestData from HTTP Request
                var httpData = ReadHttpRequestDataFromString(File.ReadAllText(inFileName));

                var verifier = new HttpSignatureVerifier<HttpRequestData>(
                    loggerFactory.CreateLogger<HttpSignatureVerifier<HttpRequestData>>())
                {
                    // Disable time check for sample sake
                    CheckSignatureTimeConstraint = false,
                    // Disable revocation check  for sample sake
                    CertificateValidationFlags = new CertificateValidationFlags
                    {
                        RevocationMode = X509RevocationMode.NoCheck,
                        VerificationFlags = X509VerificationFlags.AllFlags
                    }
                };

                verifier.VerifySignature(httpData, DateTime.Now);
            }
            catch (Exception x)
            {
                Console.Error.WriteLine(x.ToString());
            }
        }

        private static void SignHttpResponseSample(
            string algorithmName,
            string inFileName,
            string outFileName,
            X509Certificate2 publicKeyCert,
            X509Certificate2 privateKeyCert,
            ILoggerFactory loggerFactory)
        {
            try
            {
                // Get HttpRequestData from HTTP Request
                var httpData = ReadHttpResponseDataFromString(File.ReadAllText(inFileName));

                // Get certificate chain
                // If it is possible, better to cache chain, because chain creation is slow
                X509Certificate2Collection chainCertificates = GetCertificateChain(publicKeyCert);

                // Get ISigner
                var signer = SupportedAlgorithms.CreateSigner(privateKeyCert, algorithmName);

                // Create HttpSigner. Need ISigner, certificate with signer's public key and certificate chain in X509Certificate2Collection
                var reqSign = new HttpSigner<HttpResponseData>(loggerFactory.CreateLogger<HttpSigner<HttpResponseData>>())
                {
                    Signer = signer,
                    SignerCertificate = publicKeyCert,
                    SignerCertificateChain = chainCertificates
                };

                // Create signature
                reqSign.CreateSignature(httpData);

                if (reqSign.IsSignatureCreated)
                {
                    httpData.Headers.Add(HttpMessageData.DigestHeadertName, reqSign.DigestHeaderValue);
                    httpData.Headers.Add(HttpMessageData.SignatureHeaderName, reqSign.SignatureHeaderValue);

                    WriteHttpResponseDataToFile(httpData, outFileName);
                }
            }
            catch (Exception x)
            {
                Console.Error.WriteLine(x.ToString());
            }
        }

        private static void VerifyHttpResponseSignatureSample(string inFileName, ILoggerFactory loggerFactory)
        {
            try
            {
                // Get HttpResponseData from HTTP Response
                var httpData = ReadHttpResponseDataFromString(File.ReadAllText(inFileName));

                var verifier = new HttpSignatureVerifier<HttpResponseData>(loggerFactory.CreateLogger<HttpSignatureVerifier<HttpResponseData>>())
                {
                    // Disable time check for sample sake
                    CheckSignatureTimeConstraint = false,
                    // Disable revocation check  for sample sake
                    CertificateValidationFlags = new CertificateValidationFlags
                    {
                        RevocationMode = X509RevocationMode.NoCheck
                    }
                };

                verifier.VerifySignature(httpData, DateTime.Now);
            }
            catch (Exception x)
            {
                Console.Error.WriteLine(x.ToString());
            }
        }

        private static HttpResponseData ReadHttpResponseDataFromString(string text)
        {
            const string nl = "\r\n";

            HttpResponseData data = new HttpResponseData();
            var textSpan = text.AsSpan();

            // 0 - status string
            // 1 - headers
            // 2 - body
            int stage = 0;
            int startIndex = 0;
            int nlIndex;

            while ((nlIndex = text.IndexOf(nl, startIndex)) != -1)
            {
                var span = textSpan.Slice(startIndex, nlIndex - startIndex);

                if (stage == 0)
                {
                    var result = span.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    data.StatusCode = result[1].Trim();

                    stage++;
                }
                else if (stage == 1) // headers
                {
                    if (span.Length == 0)
                    {
                        // Supposedly it is a separator between headers and body
                        // Get body and break
                        data.Body = Encoding.UTF8.GetBytes(text.Substring(startIndex + 2));
                        break;
                    }
                    else
                    {
                        var indexOfColon = span.IndexOf(':');
                        var headerName = span.Slice(0, indexOfColon);
                        var headerValue = span.Slice(indexOfColon + 1).Trim();

                        data.AddHeader(headerName.ToString(), headerValue.ToString());
                    }

                }

                startIndex = nlIndex + 2; // length of "\r\n"
            }

            return data;
        }

        private static void WriteHttpResponseDataToFile(HttpResponseData d, string fileName)
        {
            const string nl = "\r\n";
            var sb = new StringBuilder();

            System.Net.HttpStatusCode x = Enum.Parse<System.Net.HttpStatusCode>(d.StatusCode);

            sb.Append($"HTTP/1.x {d.StatusCode} {x}").Append(nl);

            // Headers
            foreach (var h in d.Headers)
            {
                sb.Append($"{h.Key}: {h.Value}").Append(nl);
            }

            sb.Append(nl);

            // Body
            sb.Append(new UTF8Encoding().GetString(d.Body));

            File.WriteAllText(fileName, sb.ToString());
        }

        private static X509Certificate2Collection GetCertificateChain(X509Certificate2 cert)
        {
            var collection = new X509Certificate2Collection();

            using (var chain = new X509Chain())
            {
                chain.ChainPolicy = new X509ChainPolicy()
                {
                    RevocationMode = X509RevocationMode.NoCheck,
                    VerificationFlags = X509VerificationFlags.AllFlags,
                };

                chain.Build(cert);
                foreach (var status in chain.ChainStatus)
                {
                    if (status.Status != X509ChainStatusFlags.NoError)
                    {
                        // Keep in mind that in real application, in most cases bad chain means bad certificate.
                        Console.Error.WriteLine($"Warning: Chain build error -- {status.StatusInformation}");
                    }
                }

                int index = 0;
                foreach (var element in chain.ChainElements)
                {
                    index++;
                    // Skip first (signing cert) and last (root cert)
                    if (index == 1 || index == chain.ChainElements.Count)
                        continue;

                    collection.Add(element.Certificate);
                }
            }

            return collection;
        }

        /// <summary>
        /// Parse HTTP request from <paramref name="text"/> and creats HttpRequestData object
        /// </summary>
        /// <param name="text"></param>
        /// <returns>HttpRequestData object</returns>
        private static HttpRequestData ReadHttpRequestDataFromString(string text)
        {
            const string nl = "\r\n";

            HttpRequestData data = new HttpRequestData();
            var textSpan = text.AsSpan();

            // 0 - request target
            // 1 - host
            // 2 - headers
            // 3 - body
            int stage = 0;
            int startIndex = 0;
            int nlIndex;

            string query = string.Empty;

            while ((nlIndex = text.IndexOf(nl, startIndex)) != -1)
            {
                var span = textSpan.Slice(startIndex, nlIndex - startIndex);

                if (stage == 0)
                {
                    var result = span.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    data.Method = result[0].Trim();
                    query = result[1].Trim();
                    stage++;
                }
                else if (stage == 1) // host
                {
                    var indexOfColon = span.IndexOf(':');
                    var headerName = span.Slice(0, indexOfColon);
                    var headerValue = span.Slice(indexOfColon + 1).Trim();
                    data.Uri = new Uri($"http://{headerValue.ToString()}/{query.TrimStart('/')}");

                    data.AddHeader(headerName.ToString(), headerValue.ToString());

                    stage++;
                }
                else if (stage == 2) // headers
                {
                    if (span.Length == 0)
                    {
                        // Supposedly it is a separator between headers and body
                        // Get body and break
                        data.Body = Encoding.UTF8.GetBytes(text.Substring(startIndex + 2));
                        break;
                    }
                    else
                    {
                        var indexOfColon = span.IndexOf(':');
                        var headerName = span.Slice(0, indexOfColon);
                        var headerValue = span.Slice(indexOfColon + 1).Trim();

                        data.AddHeader(headerName.ToString(), headerValue.ToString());
                    }

                }

                startIndex = nlIndex + 2; // length of "\r\n"
            }

            return data;
        }

        private static void WriteHttpRequestDataToFile(HttpRequestData d, string fileName)
        {
            string nl = "\r\n";
            var sb = new StringBuilder();

            sb.Append($"{d.Method} {d.Uri.PathAndQuery}").Append(nl)
                .Append($"host: {d.Uri.Host}")
                .Append(nl);

            // Headers
            foreach (var h in d.Headers)
            {
                if (string.Compare(h.Key, "Host", true) == 0)
                    continue;

                sb.Append($"{h.Key}: {h.Value}").Append(nl);
            }

            sb.Append(nl);

            // Body
            sb.Append(new UTF8Encoding().GetString(d.Body));

            File.WriteAllText(fileName, sb.ToString());
        }

    }
}
