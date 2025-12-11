using LoadBalancer.Infrastructure;
using LoadBalancer.Models;

namespace LoadBalancer.Selection;

/// <summary>
/// Backend selector that routes connections in a round-robin fashion.
/// Only considers healthy backends that have not reached their connection limit.
/// Thread-safe selection using atomic index increment.
/// </summary>
public class RoundRobinBackendSelector(BackendRegistry registry) : IBackendSelector
{
    // Tracks the last used index in a thread-safe manner
    private int _index = -1;

    /// <summary>
    /// Selects the next backend for a client connection using round-robin.
    /// Returns null if no healthy backend is available.
    /// </summary>
    /// <returns>The selected BackendServer or null if none available.</returns>
    public BackendServer? PickBackendForNextConnection()
    {
        // Capture snapshot of available backends to avoid enumeration race conditions
        var available = registry.GetAllServers()
            .Where(s => s is { IsHealthy: true, HasReachedConnectionLimit: false })
            .ToList();

        if (!available.Any()) return null;

        // Increment index atomically to ensure thread-safety across multiple dispatchers
        var next = Interlocked.Increment(ref _index);

        // Round-robin selection using modulo to wrap around
        return available[next % available.Count];
    }
}