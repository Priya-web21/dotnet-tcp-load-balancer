using LoadBalancer.Infrastructure;
using LoadBalancer.Models;

namespace LoadBalancer.Selection;

/// <summary>
/// Backend selector that picks the backend with the fewest active connections.
/// Only considers healthy backends that have not reached their connection limit.
/// </summary>
public class LeastConnectionsBackendSelector : IBackendSelector
{
    private readonly BackendRegistry _registry;

    public LeastConnectionsBackendSelector(BackendRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Selects the next backend for a client connection.
    /// Returns null if no healthy backend is available.
    /// </summary>
    /// <returns>The selected BackendServer or null if none available.</returns>
    public BackendServer? PickBackendForNextConnection()
    {
        // Pick the least loaded backend among healthy servers
        return _registry.GetAllServers()
            .Where(s => s is { IsHealthy: true, HasReachedConnectionLimit: false })
            .OrderBy(s => s.ActiveConnections)
            .FirstOrDefault();
    }
}