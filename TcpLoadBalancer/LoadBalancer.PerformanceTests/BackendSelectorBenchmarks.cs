using BenchmarkDotNet.Attributes;
using LoadBalancer.Infrastructure;
using LoadBalancer.Selection;
using LoadBalancer.Settings;
using LoadBalancer.UnitTests.Helpers;

namespace LoadBalancer.PerformanceTests;

/// <summary>
/// Benchmarks backend selection strategies under simulated load.
/// </summary>
[MemoryDiagnoser]
public class BackendSelectorBenchmarks
{
    private BackendRegistry _registry;
    private LeastConnectionsBackendSelector _leastConnections;
    private RoundRobinBackendSelector _roundRobin;

    [Params(10, 100, 1000)]
    public int BackendCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var backends = new BackendSettings[BackendCount];
        for (int i = 0; i < BackendCount; i++)
        {
            backends[i] = new BackendSettings
            {
                Name = $"Server{i}",
                Host = "127.0.0.1",
                Port = 5000 + i,
                MaxConcurrentConnections = 100
            };
        }

        _registry = TestHelpers.CreateRegistry(backends);
        _leastConnections = new LeastConnectionsBackendSelector(_registry);
        _roundRobin = new RoundRobinBackendSelector(_registry);
    }

    [Benchmark]
    public void LeastConnections_SelectBackend()
    {
        _leastConnections.PickBackendForNextConnection();
    }

    [Benchmark]
    public void RoundRobin_SelectBackend()
    {
        _roundRobin.PickBackendForNextConnection();
    }
}