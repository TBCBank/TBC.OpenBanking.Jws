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

/// <summary>
/// Metadata about a signing or verification attempt. HTTP content and header values are never included.
/// </summary>
public sealed class AuditEvent
{
    internal AuditEvent(
        AuditOperation operation,
        AuditOutcome outcome,
        DateTimeOffset occurredAtUtc,
        string algorithmName,
        string certificateThumbprint,
        string failureCode)
    {
        Operation = operation;
        Outcome = outcome;
        OccurredAtUtc = occurredAtUtc;
        AlgorithmName = algorithmName;
        CertificateThumbprint = certificateThumbprint;
        FailureCode = failureCode;
    }

    public AuditOperation Operation { get; }

    public AuditOutcome Outcome { get; }

    public DateTimeOffset OccurredAtUtc { get; }

    public string AlgorithmName { get; }

    public string CertificateThumbprint { get; }

    /// <summary>
    /// Gets the exception type for a failed operation, without exception text or request data.
    /// </summary>
    public string FailureCode { get; }
}
