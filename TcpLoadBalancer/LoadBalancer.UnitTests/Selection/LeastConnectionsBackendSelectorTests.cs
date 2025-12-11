using LoadBalancer.Selection;
using LoadBalancer.Settings;
using LoadBalancer.UnitTests.Helpers;

namespace LoadBalancer.UnitTests.Selection;

/// <summary>
/// Unit tests for the LeastConnectionsBackendSelector class.
/// Validates that the selector picks the backend with the fewest connections,
/// ignores unhealthy or full backends, and handles edge cases.
/// </summary>
public class LeastConnectionsBackendSelectorTests
{
    [Fact]
    public void PickBackendForNextConnection_ReturnsServerWithLeastConnections()
    {
        // Arrange: Create registry with three servers and simulate active connections
        var registry = TestHelpers.CreateRegistry(
            new BackendSettings { Name = "Server1", Host = "127.0.0.1", Port = 1, MaxConcurrentConnections = 10 },
            new BackendSettings { Name = "Server2", Host = "127.0.0.1", Port = 2, MaxConcurrentConnections = 10 },
            new BackendSettings { Name = "Server3", Host = "127.0.0.1", Port = 3, MaxConcurrentConnections = 10 }
        );

        var servers = registry.GetAllServers().ToList();
        servers[0].RegisterNewConnection();
        servers[0].RegisterNewConnection();
        servers[1].RegisterNewConnection();

        var selector = new LeastConnectionsBackendSelector(registry);

        // Act: Pick the backend
        var selected = selector.PickBackendForNextConnection();

        // Assert: Should select the server with the fewest connections
        Assert.Equal("Server3", selected?.Name);
    }

    [Fact]
    public void PickBackendForNextConnection_SkipsUnhealthyAndFullServers()
    {
        // Arrange: Some servers unhealthy or at max connections
        var registry = TestHelpers.CreateRegistry(
            new BackendSettings { Name = "Server1", Host = "127.0.0.1", Port = 1, MaxConcurrentConnections = 10 },
            new BackendSettings { Name = "Server2", Host = "127.0.0.1", Port = 2, MaxConcurrentConnections = 1 },
            new BackendSettings { Name = "Server3", Host = "127.0.0.1", Port = 3, MaxConcurrentConnections = 10 }
        );

        var servers = registry.GetAllServers().ToList();
        servers[0].IsHealthy = false;
        servers[1].RegisterNewConnection();

        var selector = new LeastConnectionsBackendSelector(registry);

        // Act
        var selected = selector.PickBackendForNextConnection();

        // Assert: Only healthy and not full servers should be considered
        Assert.Equal("Server3", selected?.Name);
    }

    [Fact]
    public void PickBackendForNextConnection_ReturnsNullWhenNoServerAvailable()
    {
        // Arrange: All servers unhealthy
        var registry = TestHelpers.CreateRegistry(
            new BackendSettings { Name = "Server1", Host = "127.0.0.1", Port = 1, MaxConcurrentConnections = 1 },
            new BackendSettings { Name = "Server2", Host = "127.0.0.1", Port = 2, MaxConcurrentConnections = 1 }
        );

        foreach (var s in registry.GetAllServers())
            s.IsHealthy = false;

        var selector = new LeastConnectionsBackendSelector(registry);

        // Act
        var selected = selector.PickBackendForNextConnection();

        // Assert: No backend available
        Assert.Null(selected);
    }

    [Fact]
    public void PickBackendForNextConnection_WhenTie_ReturnsFirstServer()
    {
        // Arrange: Multiple servers with same load
        var registry = TestHelpers.CreateRegistry(
            new BackendSettings { Name = "Server1", Host = "127.0.0.1", Port = 1, MaxConcurrentConnections = 10 },
            new BackendSettings { Name = "Server2", Host = "127.0.0.1", Port = 2, MaxConcurrentConnections = 10 }
        );

        var selector = new LeastConnectionsBackendSelector(registry);

        // Act
        var selected = selector.PickBackendForNextConnection();

        // Assert: Ties should resolve to the first server in the list
        Assert.Equal("Server1", selected?.Name);
    }
}
