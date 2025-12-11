using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using LoadBalancer.Infrastructure;
using LoadBalancer.Logging;

namespace LoadBalancer.Services;

/// <summary>
/// Background service that listens for incoming TCP client connections.
/// Accepted connections are enqueued to the ConnectionQueue for dispatching.
/// </summary>
public class TcpListenerService : BackgroundService
{
    private readonly int _port;
    private readonly IApplicationLogger _log;
    private TcpListener? _listener;
    private readonly ConnectionQueue _queue;

    public TcpListenerService(
        IOptions<Settings.Settings> options,
        ConnectionQueue queue,
        IApplicationLoggerFactory loggerFactory)
    {
        _queue = queue;
        _port = options.Value.ListenPort;
        _log = loggerFactory.CreateLogger<TcpListenerService>();
    }

    /// <summary>
    /// Main execution loop: accepts incoming TCP clients and enqueues them for dispatch.
    /// Handles throttling and logs errors.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();

        _log.Information($"Listening on port {_port}");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for an incoming client
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);

                // Attempt to enqueue the connection, respecting global throttling
                var accepted = await _queue.TryEnqueueAsync(client, stoppingToken);

                if (!accepted)
                {
                    _log.Warning("Connection rejected due to global throttling");
                    client.Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down the service
                break;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error accepting TCP client");
            }
        }
    }

    /// <summary>
    /// Stops the TCP listener gracefully on service shutdown.
    /// </summary>
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _listener?.Stop();
        return base.StopAsync(cancellationToken);
    }
}
