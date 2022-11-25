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

#nullable enable

namespace TBC.OpenBanking.Jws.Http;

using System;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using TBC.OpenBanking.Jws;
using TBC.OpenBanking.Jws.Exceptions;

/// <summary>
///     A delegating message handler that will apply JWS signature to the outgoing request and
///     verify JWS signature on the incoming response.
/// </summary>
/// <remarks>
///     NOTICE: Must be registered as transient or scoped; NOT as singleton!
/// </remarks>
public sealed class JwsMessageHandler : DelegatingHandler
{
    private readonly IOptions<JwsClientOptions> jwsOptions;
    private readonly X509Certificate2? signerCertificate;

    private readonly HttpSigner<HttpRequestData>? reqSign;
    private readonly HttpSignatureVerifier<HttpResponseData>? verifier;

    private readonly bool doNotSign;
    private readonly bool doNotValidate;

    /// <summary>
    ///     Initializes a new instance of <see cref="JwsMessageHandler"/> class.
    /// </summary>
    /// <param name="options">
    ///     JWS options.
    /// </param>
    /// <param name="loggerFactory">
    ///     A logger factory.
    /// </param>
    /// <param name="cache">
    ///     An <see cref="IMemoryCache"/> used to cache pre-built certificate chains.
    /// </param>
    public JwsMessageHandler(
        IOptions<JwsClientOptions> options,
        ILoggerFactory loggerFactory,
        IMemoryCache cache)
    {
        _ = options ?? throw new ArgumentNullException(nameof(options));
        _ = cache ?? throw new ArgumentNullException(nameof(cache));

        this.jwsOptions = options;

        this.doNotSign = options.Value.SigningCertificate is null;
        this.doNotValidate = options.Value.ValidateSignature is false;

        if (!this.doNotSign)
        {
            this.signerCertificate = jwsOptions.Value.SigningCertificate!.GetCertificate();

            var signer = SupportedAlgorithms.CreateSigner(
                signerCertificate,
                jwsOptions.Value.AlgorithmName);

            this.reqSign = new HttpSigner<HttpRequestData>(
                loggerFactory is null
                    ? NullLoggerFactory.Instance.CreateLogger<HttpSigner<HttpRequestData>>()
                    : loggerFactory.CreateLogger<HttpSigner<HttpRequestData>>())
            {
                Signer = signer,
                SignerCertificate = signerCertificate,  // TODO: Separate publicKeyCert; or maybe dont.
                SignerCertificateChain = GetCertificateChain(this.jwsOptions, cache, signerCertificate!),
            };
        }

        if (!this.doNotValidate)
        {
            this.verifier = new HttpSignatureVerifier<HttpResponseData>(
                loggerFactory is null
                    ? NullLoggerFactory.Instance.CreateLogger<HttpSignatureVerifier<HttpResponseData>>()
                    : loggerFactory.CreateLogger<HttpSignatureVerifier<HttpResponseData>>())
            {
                CheckSignatureTimeConstraint = this.jwsOptions.Value.CheckSignatureTimeConstraint,
                CertificateValidationFlags = new CertificateValidationFlags
                {
                    RevocationMode = this.jwsOptions.Value.CheckCertificateRevocationList
                        ? X509RevocationMode.Online
                        : X509RevocationMode.NoCheck,
                }
            };
        }
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request = await this.ProcessRequestAsync(request, cancellationToken).ConfigureAwait(false);
        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await this.ProcessResponseAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpRequestMessage> ProcessRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _ = request ?? throw new ArgumentNullException(nameof(request));

        if (this.doNotSign)
        {
            return request;
        }

        var httpData = new HttpRequestData
        {
            Method = request.Method.Method,
            Uri = request.RequestUri,
        };

        // BUG: Why does this happen? Why there is no Host?
        if (string.IsNullOrWhiteSpace(request.Headers.Host))
        {
            httpData.AddHeader("Host", httpData.Uri!.DnsSafeHost);
        }

        httpData.AppendHeaders(request.Headers, true);

        if (request.Content is not null)
        {
            // This is ugly, but there's no better way (so far):

#if NET6_0_OR_GREATER
            httpData.Body = await request.Content!.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
            httpData.Body = await request.Content!.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif

            httpData.AppendHeaders(request.Content!.Headers, true);
        }

        if (this.reqSign!.CreateSignature(httpData))
        {
            httpData.Headers.Add(HttpMessageData.DigestHeadertName, this.reqSign.DigestHeaderValue);
            httpData.Headers.Add(HttpMessageData.SignatureHeaderName, this.reqSign.SignatureHeaderValue);

            request.Headers.Add(HttpMessageData.DigestHeadertName, this.reqSign.DigestHeaderValue);
            request.Headers.Add(HttpMessageData.SignatureHeaderName, this.reqSign.SignatureHeaderValue);
        }

        return request;
    }

    private async Task<HttpResponseMessage> ProcessResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        _ = response ?? throw new ArgumentNullException(nameof(response));

        if (this.doNotValidate)
        {
            return response;
        }

        // WARNING: Temporary workaround: only validate responses with 2xx status codes
        // TODO: Maybe a better way is to check for signature header existence?
        int statusCode = (int)response.StatusCode;
        if (statusCode < 300 || statusCode >= 400)
        {
            var httpData = new HttpResponseData
            {
                StatusCode = ((uint)response.StatusCode).ToString(CultureInfo.InvariantCulture)
            };

            httpData.AppendHeaders(response.Headers, true);

            if (response.Content != null)
            {
                httpData.AppendHeaders(response.Content!.Headers, true);

                // This is ugly, but there's no better way (so far):

#if NET6_0_OR_GREATER
                httpData.Body = await response.Content!.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
#else
                httpData.Body = await response.Content!.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
            }

            this.verifier!.VerifySignature(httpData, DateTime.Now);
        }

        return response;
    }

    private static X509Certificate2Collection GetCertificateChain(
        IOptions<JwsClientOptions> options,
        IMemoryCache cache,
        X509Certificate2 cert)
    {
        // Thumbprint is unique, it's a SHA1 hash
        if (cache!.TryGetValue<X509Certificate2Collection>(cert!.Thumbprint, out var collection))
        {
            return collection!;
        }

        collection = new X509Certificate2Collection();

        using (var chain = new X509Chain())
        {
            chain.ChainPolicy = new X509ChainPolicy
            {
                RevocationMode = options.Value.CheckCertificateRevocationList
                    ? X509RevocationMode.Online
                    : X509RevocationMode.NoCheck,
                VerificationFlags = X509VerificationFlags.AllFlags,
            };

            chain.Build(cert);

            foreach (var status in chain.ChainStatus)
            {
                if (status.Status != X509ChainStatusFlags.NoError)
                {
                    throw new CertificateValidationException(chain.ChainStatus, "Chain build error");
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

        var entryOptions = new MemoryCacheEntryOptions()
            .SetPriority(CacheItemPriority.High)
            .SetSlidingExpiration(TimeSpan.FromMinutes(20d))  // TODO: Make configurable
            // Average approximate size of the chain; we don't want to calculate the actual size
            // because touching cert.RawData will cause allocations (!) -- it will return a copy
            // (Does not have to be an actual size, just a guess is sufficient)
            .SetSize(5000L);

        cache.Set<X509Certificate2Collection>(cert.Thumbprint, collection, entryOptions);

        return collection;
    }
}
