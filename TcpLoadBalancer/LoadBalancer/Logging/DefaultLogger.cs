using Microsoft.Extensions.Logging;

namespace LoadBalancer.Logging;

/// <summary>
/// Simple wrapper around Microsoft.Extensions.Logging.ILogger
/// implementing the IApplicationLogger interface.
/// Provides a consistent logging abstraction for the application.
/// </summary>
public sealed class DefaultLogger : IApplicationLogger
{
    private readonly ILogger _logger;

    public DefaultLogger(ILogger logger) => _logger = logger;

    /// <summary>
    /// Logs a debug-level message.
    /// </summary>
    public void Debug(string message) =>
        _logger.LogDebug(message);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public void Information(string message) =>
        _logger.LogInformation(message);

    /// <summary>
    /// Logs a warning-level message.
    /// </summary>
    public void Warning(string message) =>
        _logger.LogWarning(message);

    /// <summary>
    /// Logs an error with exception details.
    /// </summary>
    /// <param name="ex">The exception being logged</param>
    /// <param name="message">Additional contextual message</param>
    public void Error(Exception ex, string message) =>
        _logger.LogError(ex, message);
}