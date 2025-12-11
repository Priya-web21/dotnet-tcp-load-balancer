using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using LoadBalancer.Logging;

namespace LoadBalancer.Infrastructure;

/// <summary>
/// Thread-safe, bounded queue for managing incoming TCP client connections.
/// Uses a Channel to allow multiple producers (TCP listener) and multiple consumers (DispatcherService).
/// </summary>
public class ConnectionQueue
{
    // Underlying channel for thread-safe connection storage
    private readonly Channel<TcpClient> _channel;

    // Application logger for observability
    private readonly IApplicationLogger _logger;

    /// <summary>
    /// Initializes a new instance of ConnectionQueue/>.
    /// </summary>
    /// <param name="options">Settings containing MaxGlobalConnections</param>
    /// <param name="loggerFactory">Logger factory for creating application logger</param>
    public ConnectionQueue(
        IOptions<Settings.Settings> options,
        IApplicationLoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ConnectionQueue>();

        var limit = options.Value.MaxGlobalConnections;

        // Configure a bounded channel to throttle connections when queue is full
        var channelOptions = new BoundedChannelOptions(limit)
        {
            // Wait for space to become available if full
            FullMode = BoundedChannelFullMode.Wait,
            // Allow multiple readers and writers for concurrent access
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<TcpClient>(channelOptions);

        _logger.Information($"ConnectionQueue initialized with max capacity {limit}");
    }

    /// <summary>
    /// Writer for enqueuing new TCP client connections.
    /// </summary>
    public ChannelWriter<TcpClient> Writer => _channel.Writer;

    /// <summary>
    /// Reader for dequeuing TCP client connections for processing.
    /// </summary>
    public ChannelReader<TcpClient> Reader => _channel.Reader;

    /// <summary>
    /// Attempts to enqueue a new TCP client connection into the bounded queue.
    /// Returns false if the queue is full or write could not complete.
    /// </summary>
    /// <param name="client">TCP client to enqueue</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>True if successfully enqueued; false otherwise</returns>
    public async ValueTask<bool> TryEnqueueAsync(TcpClient client, CancellationToken token)
    {
        // Wait until space is available or cancellation is requested
        if (await _channel.Writer.WaitToWriteAsync(token))
        {
            if (_channel.Writer.TryWrite(client))
            {
                return true;
            }

            // Queue is full; log warning
            _logger.Warning("Connection queue rejected a write due to saturation");
        }

        return false;
    }
}
