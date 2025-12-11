namespace LoadBalancer.Logging;

public interface IApplicationLogger
{
    void Debug(string message);
    void Information(string message);
    void Warning(string message);
    void Error(Exception ex, string message);
}