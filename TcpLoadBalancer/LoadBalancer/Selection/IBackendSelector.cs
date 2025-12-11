using LoadBalancer.Models;

namespace LoadBalancer.Selection;

public interface IBackendSelector
{
    BackendServer? PickBackendForNextConnection();
}