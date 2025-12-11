using BenchmarkDotNet.Running;

namespace LoadBalancer.PerformanceTests;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<BackendSelectorBenchmarks>();
    }
}