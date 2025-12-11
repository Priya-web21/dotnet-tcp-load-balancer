using Microsoft.Extensions.Logging;

namespace LoadBalancer.Logging;

/// <summary>
/// Factory for creating application loggers implementing IApplicationLogger.
/// Wraps Microsoft.Extensions.Logging.ILoggerFactory to provide typed loggers.
/// </summary>
public sealed class ApplicationLoggerFactory : IApplicationLoggerFactory
{
    private readonly ILoggerFactory _factory;

    public ApplicationLoggerFactory(ILoggerFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Creates a typed application logger for the specified class type.
    /// </summary>
    /// <typeparam name="T">The class type to associate with the logger.</typeparam>
    /// <returns>IApplicationLogger instance for logging</returns>
    public IApplicationLogger CreateLogger<T>()
    {
        return new DefaultLogger(_factory.CreateLogger<T>());
    }
}