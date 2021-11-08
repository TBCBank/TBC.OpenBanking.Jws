namespace WebApiHttpClientExample
{
    using System;
    using System.Net;
    using System.Net.Http;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.OpenApi.Models;
    using TBC.OpenBanking.Jws;
    using TBC.OpenBanking.Jws.Http;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            //
            // Add ability to parse and convert cert:// and pfx:// URLs
            // (Required for JwsClientOptions deserialization from appsettings to work)
            //
            X509CertificateLocator.RegisterUriParsers();

            //
            // Read JWS options from appsettings.json
            //
            services.AddOptions<JwsClientOptions>()
                .BindConfiguration("Jws:Options");

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApiHttpClientExample", Version = "v1" });
            });

            //
            // Memory cache is required by JwsMessageHandler
            //
            services.AddMemoryCache();

            //
            // HttpClient for TBC Bank's Open Banking API
            //
            services.AddHttpClient(name: "TBC.OpenBanking.Client", configureClient: (client) =>
            {
                client.BaseAddress = new Uri(this.Configuration["OpenBankingConnection:BaseUrl"]);
            }).ConfigurePrimaryHttpMessageHandler((svcs) =>
            {
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.All,
                    MaxConnectionsPerServer = 4,
                    UseCookies = false,
                    UseProxy = false,
                };

                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ClientCertificates.Add(
                    new X509CertificateLocator(
                        new Uri(this.Configuration["OpenBankingConnection:ClientCertificate"])).GetCertificate());

                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                return handler;
            })
            .AddHttpMessageHandler((svcs) =>
            {
                //
                // Insert JWS message handler into the handler pipeline
                // JwsMessageHandler will sign requests and validate responses
                //

                var options = svcs.GetRequiredService<IOptions<JwsClientOptions>>();

                var loggerFactory = svcs.GetRequiredService<ILoggerFactory>();
                var memoryCache = svcs.GetRequiredService<IMemoryCache>();

                return new JwsMessageHandler(options, loggerFactory, memoryCache);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApiHttpClientExample v1"));
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
