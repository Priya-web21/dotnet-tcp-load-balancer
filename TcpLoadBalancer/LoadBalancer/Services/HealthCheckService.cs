using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using LoadBalancer.Infrastructure;
using LoadBalancer.Logging;
using LoadBalancer.Models;

namespace LoadBalancer.Services;

/// <summary>
/// Periodically probes backend servers to determine health status.
/// Updates the IsHealthy property for routing decisions.
/// Runs as a background service and handles backends concurrently.
/// </summary>
public class HealthCheckService : BackgroundService
{
    private readonly BackendRegistry _registry;
    private readonly IApplicationLogger _logger;
    private readonly TimeSpan _interval;
    private readonly int _timeoutMs;

    public HealthCheckService(
        BackendRegistry registry,
        IOptions<Settings.Settings> settings,
        IApplicationLoggerFactory loggerFactory)
    {
        _registry = registry;
        _logger = loggerFactory.CreateLogger<HealthCheckService>();
        _interval = TimeSpan.FromSeconds(settings.Value.HealthChecks.IntervalSeconds);
        _timeoutMs = settings.Value.HealthChecks.TimeoutMilliseconds;
    }

    /// <summary>
    /// Main execution loop: periodically probes all backends concurrently.
    /// Uses a snapshot to ensure thread-safe enumeration.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.Information("Health check service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Capture a snapshot of backends for thread-safe enumeration
            var backendsSnapshot = _registry.GetAllServers();

            // Probe all backends concurrently to reduce total delay
            var probeTasks = backendsSnapshot.Select(backend =>
                ProbeBackendAsync(backend, stoppingToken));

            await Task.WhenAll(probeTasks);

            // Wait before next health check round
            await Task.Delay(_interval, stoppingToken);
        }
    }

    /// <summary>
    /// Probes a single backend server using TCP connect with a timeout.
    /// Updates the backend's IsHealthy property based on the result.
    /// </summary>
    private async Task ProbeBackendAsync(BackendServer backend, CancellationToken token)
    {
        try
        {
            using var client = new TcpClient();

            // Combine global cancellation token with timeout for the probe
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                token,
                new CancellationTokenSource(_timeoutMs).Token
            );

            await client.ConnectAsync(backend.Host, backend.Port, cts.Token);

            if (!backend.IsHealthy)
            {
                backend.IsHealthy = true;
                _logger.Information($"Backend '{backend.Name}' marked HEALTHY");
            }
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            // Handle expected network or timeout failures
            MarkBackendUnhealthy(backend);
        }
        catch (Exception ex)
        {
            // Handle any other unexpected exceptions
            _logger.Error(ex, $"Unexpected health check failure for {backend.Name}");
            MarkBackendUnhealthy(backend);
        }
    }

    /// <summary>
    /// Marks a backend as unhealthy if it was previously healthy.
    /// </summary>
    private void MarkBackendUnhealthy(BackendServer backend)
    {
        if (backend.IsHealthy)
        {
            backend.IsHealthy = false;
            _logger.Warning($"Backend '{backend.Name}' marked UNHEALTHY");
        }
    }
}
