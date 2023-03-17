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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal sealed class ConsoleApp : IHostedService
{
    private readonly ILogger<ConsoleApp> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IServiceScope _serviceScope;

    public ConsoleApp(
        ILogger<ConsoleApp> logger,
        IHostApplicationLifetime appLifetime,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
        _serviceScope = serviceProvider.CreateScope();
    }

    private async Task RunAsync(IServiceProvider services)
    {
        //
        // Resolve named HttpClient from service provider:
        //
        var clientFactory = services.GetRequiredService<IHttpClientFactory>();
        using var httpClient = clientFactory.CreateClient("ExampleClient");

        _logger.LogInformation("Sending request...");

        //
        // Add mandatory X-Request-ID header:
        //
        httpClient.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString("N"));

        var response = await httpClient.PostAsync("/post", new StringContent("{ \"Hello\": \"World\" }"));

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        //
        // HTTPBin returns JSON structure describing request we've sent:
        //
        _logger.LogInformation("Response from HTTPBin: {Content}", content);

        await Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async () =>
            {
                try
                {
                    await RunAsync(_serviceScope.ServiceProvider);
                }
                catch (Exception error)
                {
                    _logger.LogError(error, "Unhandled exception!");
                }
                finally
                {
                    _serviceScope?.Dispose();
                    _appLifetime.StopApplication();
                }
            });
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
