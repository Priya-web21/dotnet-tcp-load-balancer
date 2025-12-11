namespace LoadBalancer.Logging;

public interface IApplicationLoggerFactory
{
    IApplicationLogger CreateLogger<T>();
}