namespace LoadBalancer.Models;

/// <summary>
/// Represents a backend server node in the pool.
/// Tracks health and active connections in a thread-safe way.
/// </summary>
public sealed class BackendServer
{
    public string Name { get; init; } = "";
    public string Host { get; init; } = "";
    public int Port { get; init; }
    public int MaxConcurrentConnections { get; init; }

    /// <summary>
    /// Indicates whether the backend is healthy and can receive traffic.
    /// </summary>
    public volatile bool IsHealthy = true;

    private int _activeConnections;

    /// <summary>
    /// Current number of active connections to this backend.
    /// Thread-safe read using Volatile.
    /// </summary>
    public int ActiveConnections => Volatile.Read(ref _activeConnections);

    /// <summary>
    /// Increment the active connection count (thread-safe).
    /// Call when a new client is routed to this backend.
    /// </summary>
    public void RegisterNewConnection() =>
        Interlocked.Increment(ref _activeConnections);

    /// <summary>
    /// Decrement the active connection count (thread-safe).
    /// Call when a client connection completes.
    /// </summary>
    public void CompleteConnection() =>
        Interlocked.Decrement(ref _activeConnections);

    /// <summary>
    /// Returns true if the backend has reached its max concurrent connections.
    /// </summary>
    public bool HasReachedConnectionLimit =>
        ActiveConnections >= MaxConcurrentConnections;
}