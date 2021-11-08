using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TBC.OpenBanking.Jws;
using TBC.OpenBanking.Jws.Http;

static class Program
{
    static async Task Main(string[] args)
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
                .UseConsoleLifetime()
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
                        var options = services.GetRequiredService<IOptions<JwsClientOptions>>();

                        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
                        var cache = services.GetRequiredService<IMemoryCache>();

                        return new JwsMessageHandler(options, loggerFactory, cache);
                    });

                    services.AddHostedService<ConsoleApp>();
                })
                .RunConsoleAsync();
        }
    }
}
