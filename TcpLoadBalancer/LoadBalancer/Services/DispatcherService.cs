using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using LoadBalancer.Logging;
using LoadBalancer.Selection;
using LoadBalancer.Infrastructure;
using LoadBalancer.Models;

namespace LoadBalancer.Services;

/// <summary>
/// Background service that dispatches incoming client connections to available backends.
/// Pulls connections from the ConnectionQueue and routes them using the configured IBackendSelector.
/// Designed for high concurrency and non-blocking operation.
/// </summary>
public class DispatcherService : BackgroundService
{
    private readonly ConnectionQueue _queue;
    private readonly IBackendSelector _selector;
    private readonly IApplicationLogger _logger;

    public DispatcherService(
        ConnectionQueue queue,
        IBackendSelector selector,
        IApplicationLoggerFactory loggerFactory)
    {
        _queue = queue;
        _selector = selector;
        _logger = loggerFactory.CreateLogger<DispatcherService>();
    }

    /// <summary>
    /// Continuously reads incoming TCP clients from the queue and dispatches them asynchronously.
    /// Fire-and-forget is used to avoid blocking the queue processing.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var client in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            // Start dispatch in background without awaiting to handle multiple connections concurrently
            _ = DispatchConnectionAsync(client, stoppingToken);
        }
    }

    /// <summary>
    /// Dispatches a single client connection to a selected backend.
    /// Handles backend selection, connection tracking, proxying, error logging, and cleanup.
    /// </summary>
    /// <param name="client">Incoming TCP client</param>
    /// <param name="token">Cancellation token for graceful shutdown</param>
    private async Task DispatchConnectionAsync(TcpClient client, CancellationToken token)
    {
        BackendServer? backend = null;

        try
        {
            // Select backend based on strategy (LeastConnections/RoundRobin)
            backend = _selector.PickBackendForNextConnection();

            if (backend == null)
            {
                // No healthy backend available; drop the connection
                _logger.Warning("No healthy backend available");
                client.Dispose();
                return;
            }

            // Increment active connection count for backend
            backend.RegisterNewConnection();

            _logger.Information($"Routed connection to {backend.Name}");

            // Proxy traffic between client and backend
            await TcpProxySession.ProxyTrafficAsync(client, backend, token);
        }
        catch (Exception ex)
        {
            // Log dispatch or proxy errors
            _logger.Error(ex, "Dispatch failure");
        }
        finally
        {
            // Decrement active connection count and dispose client
            backend?.CompleteConnection();
            client.Dispose();
        }
    }
}
