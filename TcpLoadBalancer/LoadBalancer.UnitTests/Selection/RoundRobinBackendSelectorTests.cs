using LoadBalancer.Models;
using LoadBalancer.Selection;
using LoadBalancer.Settings;
using LoadBalancer.UnitTests.Helpers;

namespace LoadBalancer.UnitTests.Selection;

/// <summary>
/// Unit tests for RoundRobinBackendSelector.
/// Ensures proper round-robin rotation, skips unhealthy/full backends,
/// handles no available backends, and validates thread-safety.
/// </summary>
public class RoundRobinBackendSelectorTests
{
    [Fact]
    public void PickBackendForNextConnection_ShouldRotateThroughServers()
    {
        // Arrange: Three healthy backends
        var registry = TestHelpers.CreateRegistry(
            new BackendSettings { Name = "A", Host = "127.0.0.1", Port = 1, MaxConcurrentConnections = 10 },
            new BackendSettings { Name = "B", Host = "127.0.0.1", Port = 2, MaxConcurrentConnections = 10 },
            new BackendSettings { Name = "C", Host = "127.0.0.1", Port = 3, MaxConcurrentConnections = 10 }
        );

        var selector = new RoundRobinBackendSelector(registry);

        // Act: Pick backends consecutively
        var s1 = selector.PickBackendForNextConnection();
        var s2 = selector.PickBackendForNextConnection();
        var s3 = selector.PickBackendForNextConnection();
        var s4 = selector.PickBackendForNextConnection();

        // Assert: Should rotate in order
        Assert.Equal("A", s1?.Name);
        Assert.Equal("B", s2?.Name);
        Assert.Equal("C", s3?.Name);
        Assert.Equal("A", s4?.Name);
    }

    [Fact]
    public void PickBackendForNextConnection_ShouldSkipUnhealthyServers()
    {
        // Arrange: Mark one backend as unhealthy
        var registry = TestHelpers.CreateRegistry(
            new BackendSettings { Name = "A", Host = "127.0.0.1", Port = 1, MaxConcurrentConnections = 10 },
            new BackendSettings { Name = "B", Host = "127.0.0.1", Port = 2, MaxConcurrentConnections = 10 },
            new BackendSettings { Name = "C", Host = "127.0.0.1", Port = 3, MaxConcurrentConnections = 10 }
        );

        var servers = registry.GetAllServers().ToList();
        servers[1].IsHealthy = false;

        var selector = new RoundRobinBackendSelector(registry);

        // Act: Pick backends consecutively
        var s1 = selector.PickBackendForNextConnection();
        var s2 = selector.PickBackendForNextConnection();
        var s3 = selector.PickBackendForNextConnection();

        // Assert: Unhealthy backend should be skipped
        Assert.Equal("A", s1?.Name);
        Assert.Equal("C", s2?.Name);
        Assert.Equal("A", s3?.Name);
    }

    [Fact]
    public void PickBackendForNextConnection_ShouldSkipFullServers()
    {
        // Arrange: First backend at max connections
        var registry = TestHelpers.CreateRegistry(
            new BackendSettings { Name = "A", Host = "127.0.0.1", Port = 1, MaxConcurrentConnections = 1 },
            new BackendSettings { Name = "B", Host = "127.0.0.1", Port = 2, MaxConcurrentConnections = 10 }
        );

        var servers = registry.GetAllServers().ToList();
        servers[0].RegisterNewConnection();

        var selector = new RoundRobinBackendSelector(registry);

        // Act: Pick backends
        var first = selector.PickBackendForNextConnection();
        var second = selector.PickBackendForNextConnection();

        // Assert: Full backend should be skipped
        Assert.Equal("B", first?.Name);
        Assert.Equal("B", second?.Name);
    }

    [Fact]
    public void PickBackendForNextConnection_ShouldReturnNull_WhenNoServersAvailable()
    {
        // Arrange: All backends unhealthy
        var registry = TestHelpers.CreateRegistry(
            new BackendSettings { Name = "A", Host = "127.0.0.1", Port = 1, MaxConcurrentConnections = 1 },
            new BackendSettings { Name = "B", Host = "127.0.0.1", Port = 2, MaxConcurrentConnections = 1 }
        );

        foreach (var s in registry.GetAllServers())
            s.IsHealthy = false;

        var selector = new RoundRobinBackendSelector(registry);

        // Act
        var result = selector.PickBackendForNextConnection();

        // Assert: No backend available
        Assert.Null(result);
    }

    [Fact]
    public void PickBackendForNextConnection_ShouldBeThreadSafe()
    {
        // Arrange: Two healthy backends
        var registry = TestHelpers.CreateRegistry(
            new BackendSettings { Name = "A", Host = "127.0.0.1", Port = 1, MaxConcurrentConnections = 10 },
            new BackendSettings { Name = "B", Host = "127.0.0.1", Port = 2, MaxConcurrentConnections = 10 }
        );

        var selector = new RoundRobinBackendSelector(registry);

        // Act: Pick backends concurrently
        var results = new BackendServer?[100];
        Parallel.For(0, 100, i =>
        {
            results[i] = selector.PickBackendForNextConnection();
        });

        // Assert: All results should be non-null
        Assert.All(results, r => Assert.NotNull(r));
    }
}
