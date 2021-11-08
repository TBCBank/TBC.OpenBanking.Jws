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

namespace TBC.OpenBanking.Jws.Http
{
    using Microsoft.Extensions.Options;

    public sealed class JwsClientOptions : IOptions<JwsClientOptions>
    {
        public bool Enabled
        {
            get; set;
        } = true;

        /// <summary>
        /// See <see cref="SupportedAlgorithms"/> class for possible values.
        /// </summary>
        public string? AlgorithmName
        {
            get; set;
        } = "RS256";

        public X509CertificateLocator? SigningCertificate
        {
            get; set;
        }

        public bool CheckSignatureTimeConstraint
        {
            get; set;
        } = true;

        public bool CheckCertificateRevocationList
        {
            get; set;
        } = true;

        public bool ValidateSignature
        {
            get; set;
        } = true;

        public JwsClientOptions Value => this;
    }
}
