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

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TBC.OpenBanking.Jws;
using TBC.OpenBanking.Jws.Http;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please specify certificate location");

            Console.WriteLine("\nExamples of certificate location URLs");
            Console.WriteLine("cert:///CurrentUser/My/C594DAD7CBCBD6CC9704F4F463EABE68980F640F");
            Console.WriteLine("pfx://PASSWORD@local/C:\\Secrets\\Jws.pfx");
            // Passwordless PFX file:
            Console.WriteLine("pfx:///C:\\Secrets\\Jws.pfx");

            await Task.FromResult(1);
        }
        else
        {
            var certLocation = args[0];

            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMemoryCache();  // required by JwsMessageHandler

                    services.Configure<JwsClientOptions>(options =>
                    {
                        options.AlgorithmName = SupportedAlgorithms.RsaPKCS1Sha256;

                        //
                        // Disable all validations because we're calling httpbin.org,
                        // which does not sign responses with JWS; it just returns what we've sent:
                        //
                        options.CheckCertificateRevocationList = false;
                        options.CheckSignatureTimeConstraint = false;
                        options.ValidateSignature = false;

                        Uri certUrl = new(certLocation);
                        options.SigningCertificate = new X509CertificateLocator(certUrl);
                    });

                    services.AddHttpClient(name: "ExampleClient", configureClient: (client) =>
                    {
                        //
                        // httpbin.org is a free service used to test HTTP request/responses;
                        // It simply reflects the request you've sent back to you:
                        //
                        client.BaseAddress = new("https://httpbin.org/");
                    })
                    .AddHttpMessageHandler(services =>
                    {
                        var options       = services.GetRequiredService<IOptions<JwsClientOptions>>();
                        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                        var cache         = services.GetRequiredService<IMemoryCache>();

                        return new JwsMessageHandler(options, loggerFactory, cache);
                    });

                    services.AddHostedService<ConsoleApp>();
                })
                .RunConsoleAsync();
        }
    }
}
