using LoadBalancer.Logging;

namespace LoadBalancer.UnitTests.Logging;

/// <summary>
/// A simple test logger that does nothing. 
/// Can be extended to capture logs for verification in tests.
/// </summary>
public class TestLogger : IApplicationLogger
{
    public void Debug(string message) { }
    public void Information(string message) { }
    public void Warning(string message) { }
    public void Error(Exception ex, string message) { }
}

/// <summary>
/// A test logger factory that always returns a TestLogger instance.
/// Used to simplify dependency injection in unit tests.
/// </summary>
public class TestLoggerFactory : IApplicationLoggerFactory
{
    public IApplicationLogger CreateLogger<T>() => new TestLogger();
}