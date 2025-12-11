using System.Net.Sockets;
using LoadBalancer.Models;

namespace LoadBalancer.Services;

/// <summary>
/// Handles bidirectional TCP proxying between a client and a backend server.
/// Acts as a simple layer-4 forwarder without inspecting payload.
/// </summary>
public static class TcpProxySession
{
    /// <summary>
    /// Proxies traffic between the client and backend streams until either side closes.
    /// </summary>
    /// <param name="client">The accepted TCP client connection.</param>
    /// <param name="backend">The backend server to forward traffic to.</param>
    /// <param name="token">Cancellation token to stop the proxy session.</param>
    public static async Task ProxyTrafficAsync(TcpClient client, BackendServer backend, CancellationToken token)
    {
        // Establish connection to backend server
        using var backendClient = new TcpClient();
        await backendClient.ConnectAsync(backend.Host, backend.Port, token);

        using var clientStream = client.GetStream();
        using var backendStream = backendClient.GetStream();

        // Start bidirectional copy; completes when either side closes or token is canceled
        await Task.WhenAny(
            clientStream.CopyToAsync(backendStream, token),
            backendStream.CopyToAsync(clientStream, token));

        // No explicit disposal needed for streams; using declarations ensure cleanup
    }
}