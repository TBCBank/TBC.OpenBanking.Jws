using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

class ConsoleApp : IHostedService
{
    readonly ILogger<ConsoleApp> _logger;
    readonly IHostApplicationLifetime _appLifetime;
    readonly IServiceScope _serviceScope;

    public ConsoleApp(
        ILogger<ConsoleApp> logger,
        IHostApplicationLifetime appLifetime,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
        _serviceScope = serviceProvider.CreateScope();
    }

    async Task RunAsync(IServiceProvider services)
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
