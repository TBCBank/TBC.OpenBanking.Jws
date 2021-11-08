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

namespace TBC.OpenBanking.Jws.Exceptions
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    [Serializable]
    public class CertificateValidationException : JwsException
    {
        private const int ErrorCode = 102;
        private readonly string message;

        public CertificateValidationException(string message)
            : base(message)
        {
            this.SetHResult(ErrorCode);
        }

        public CertificateValidationException(X509ChainStatus[] statuses, string message)
        {
            this.SetHResult(ErrorCode);

            if (statuses != null)
            {
                var sb = new StringBuilder();
                sb.Append(message).Append(". ");

                int i = 0;
                foreach (var status in statuses)
                {
                    sb.Append(++i)
                        .Append(". Status: ")
                        .Append(status.Status.ToString())
                        .Append(" Desc: ")
                        .Append(status.StatusInformation)
                        .Append("; ");
                }

                this.message = sb.ToString();
            }
            else
                this.message = message;
        }

        public override string Message { get => this.message; }

        public CertificateValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
            this.SetHResult(ErrorCode);
        }

        public CertificateValidationException()
        {
            this.SetHResult(ErrorCode);
        }
    }
}
