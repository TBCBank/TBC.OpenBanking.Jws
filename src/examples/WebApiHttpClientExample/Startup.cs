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

namespace WebApiHttpClientExample;

using System;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        // Read JWS options from IConfiguration
        //
        services.AddOptions<JwsClientOptions>().BindConfiguration("Jws:Options");

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
        // JwsMessageHandler is used to sign requests and validate responses
        //
        services.AddTransient<JwsMessageHandler>();

        //
        // HttpClient for TBC Bank's Open Banking API
        //
        services.AddHttpClient(name: "TBC.OpenBanking.Client", configureClient: (client) =>
        {
            client.BaseAddress = new Uri(this.Configuration["OpenBankingConnection:BaseUrl"]);
        })
            .ConfigurePrimaryHttpMessageHandler((svcs) =>
            {
                var handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.All,
                    UseCookies = false,
                };

                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.ClientCertificates.Add(new X509CertificateLocator(
                    new Uri(this.Configuration["OpenBankingConnection:ClientCertificate"])).GetCertificate());

                // DONT use this in production code:
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                return handler;
            })
            .AddHttpMessageHandler<JwsMessageHandler>();  // Handler to sign requests and validate responses
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
