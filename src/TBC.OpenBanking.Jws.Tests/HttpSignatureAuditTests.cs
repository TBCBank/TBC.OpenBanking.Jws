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

namespace TBC.OpenBanking.Jws.Tests;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using TBC.OpenBanking.Jws.Exceptions;
using Xunit;

public sealed class HttpSignatureAuditTests
{
    [Fact]
    public void CreateSignature_WritesSuccessEventWithoutRequestContent()
    {
        using var certificate = CreateCertificate();
        var sink = new RecordingAuditSink();
        var signer = CreateSigner(certificate, sink);
        var request = CreateRequest();

        Assert.True(signer.CreateSignature(request));

        AuditEvent auditEvent = Assert.Single(sink.Events);
        Assert.Equal(AuditOperation.Signing, auditEvent.Operation);
        Assert.Equal(AuditOutcome.Succeeded, auditEvent.Outcome);
        Assert.Equal(SupportedAlgorithms.RsaPKCS1Sha256, auditEvent.AlgorithmName);
        Assert.Equal(certificate.Thumbprint, auditEvent.CertificateThumbprint);
        Assert.Null(auditEvent.FailureCode);
        Assert.Equal(TimeSpan.Zero, auditEvent.OccurredAtUtc.Offset);
        string[] signatureParts = signer.SignatureHeaderValue.Split(
            "..",
            StringSplitOptions.RemoveEmptyEntries);
        string payload = request.ComposeHeadersForSignature(
            signer.ProtectedHeader.DataToBeSigned.Parameters,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [HttpMessageData.DigestHeadertName] = signer.DigestHeaderValue,
            });
        using Algorithm signatureVerifier = SupportedAlgorithms.CreateVerifier(
            certificate,
            SupportedAlgorithms.RsaPKCS1Sha256);
        Assert.True(signatureVerifier.VerifySignature(signatureParts[0], payload, signatureParts[1]));
        Assert.DoesNotContain(
            auditEvent.GetType().GetProperties(),
            property => property.Name.Contains("Body", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Header", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateSignature_WhenRequiredHeaderIsMissing_WritesFailureAndClearsPreviousResult()
    {
        using var certificate = CreateCertificate();
        var sink = new RecordingAuditSink();
        var signer = CreateSigner(certificate, sink);
        Assert.True(signer.CreateSignature(CreateRequest()));

        Assert.Throws<HeaderMissingException>(() => signer.CreateSignature(new HttpRequestData
        {
            Method = "POST",
            Uri = new Uri("https://api.example.test/v1/consents"),
        }));

        Assert.Equal(2, sink.Events.Count);
        AuditEvent auditEvent = sink.Events[1];
        Assert.Equal(AuditOutcome.Failed, auditEvent.Outcome);
        Assert.Equal(nameof(HeaderMissingException), auditEvent.FailureCode);
        Assert.False(signer.IsSignatureCreated);
        Assert.Null(signer.SignatureHeaderValue);
        Assert.Null(signer.DigestHeaderValue);
    }

    [Fact]
    public void VerifySignature_WhenBodyWasChanged_WritesFailureEventAndRethrows()
    {
        using var certificate = CreateCertificate();
        var signingSink = new RecordingAuditSink();
        var signer = CreateSigner(certificate, signingSink);
        HttpRequestData request = CreateRequest();
        Assert.True(signer.CreateSignature(request));
        request.AddHeader(HttpMessageData.DigestHeadertName, signer.DigestHeaderValue);
        request.AddHeader(HttpMessageData.SignatureHeaderName, signer.SignatureHeaderValue);
        request.Body = Encoding.UTF8.GetBytes("{\"amount\":999}");

        var verificationSink = new RecordingAuditSink();
        var verifier = new HttpSignatureVerifier<HttpRequestData>(
            NullLogger<HttpSignatureVerifier<HttpRequestData>>.Instance,
            verificationSink)
        {
            CheckSignatureTimeConstraint = false,
        };

        Assert.Throws<SignatureVerificationProblemException>(
            () => verifier.VerifySignature(request, DateTime.UtcNow));

        AuditEvent auditEvent = Assert.Single(verificationSink.Events);
        Assert.Equal(AuditOperation.Verification, auditEvent.Operation);
        Assert.Equal(AuditOutcome.Failed, auditEvent.Outcome);
        Assert.Equal(SupportedAlgorithms.RsaPKCS1Sha256, auditEvent.AlgorithmName);
        Assert.Equal(nameof(SignatureVerificationProblemException), auditEvent.FailureCode);
    }

    [Fact]
    public void CreateSignature_WhenAuditSinkFails_DoesNotReportSuccess()
    {
        using var certificate = CreateCertificate();
        var signer = CreateSigner(certificate, new ThrowingAuditSink());

        Assert.Throws<InvalidOperationException>(() => signer.CreateSignature(CreateRequest()));
    }

    private static HttpSigner<HttpRequestData> CreateSigner(X509Certificate2 certificate, IAuditSink auditSink)
    {
        return new HttpSigner<HttpRequestData>(
            NullLogger<HttpSigner<HttpRequestData>>.Instance,
            auditSink)
        {
            Signer = SupportedAlgorithms.CreateSigner(certificate, SupportedAlgorithms.RsaPKCS1Sha256),
            SignerCertificate = certificate,
            SignerCertificateChain = new X509Certificate2Collection(),
        };
    }

    private static HttpRequestData CreateRequest()
    {
        var request = new HttpRequestData
        {
            Method = "POST",
            Uri = new Uri("https://api.example.test/v1/consents"),
            Body = Encoding.UTF8.GetBytes("{\"amount\":10}"),
        };
        request.AddHeader("host", "api.example.test");
        request.AddHeader("x-request-id", "test-request-001");
        request.AddHeader("content-type", "application/json");
        return request;
    }

    private static X509Certificate2 CreateCertificate()
    {
        using RSA rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=JWS Test,O=Example",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddDays(1));
    }

    private sealed class RecordingAuditSink : IAuditSink
    {
        public List<AuditEvent> Events { get; } = [];

        public void Write(AuditEvent auditEvent) => Events.Add(auditEvent);
    }

    private sealed class ThrowingAuditSink : IAuditSink
    {
        public void Write(AuditEvent auditEvent) => throw new InvalidOperationException("Audit store unavailable.");
    }
}
