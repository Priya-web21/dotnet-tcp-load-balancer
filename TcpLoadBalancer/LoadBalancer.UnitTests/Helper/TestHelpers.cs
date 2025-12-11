using LoadBalancer.Infrastructure;
using LoadBalancer.Settings;
using Microsoft.Extensions.Options;
using LoadBalancer.UnitTests.Logging;

namespace LoadBalancer.UnitTests.Helpers;

/// <summary>
/// Provides helper methods for creating test objects and configurations.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a BackendRegistry pre-populated with the given backends and a test logger.
    /// </summary>
    /// <param name="backends">Array of backend settings to populate the registry.</param>
    /// <returns>BackendRegistry instance with the specified backends.</returns>
    public static BackendRegistry CreateRegistry(params BackendSettings[] backends)
    {
        var settings = new Settings.Settings
        {
            Backends = backends.ToList()
        };

        var options = Options.Create(settings);
        var loggerFactory = new TestLoggerFactory();

        return new BackendRegistry(options, loggerFactory);
    }
}