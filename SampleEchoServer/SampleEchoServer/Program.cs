using System.Net;
using System.Net.Sockets;

const int port = 6002;

var listener = new TcpListener(IPAddress.Any, port);
listener.Start();

Console.WriteLine($"Echo server listening on port {port}");

while (true)
{
    var client = await listener.AcceptTcpClientAsync();

    // Handle each client in its own task
    _ = Task.Run(async () =>
    {
        using (client)
        {
            var stream = client.GetStream();
            var buffer = new byte[4096];

            while (true)
            {
                int bytesRead;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer);
                    if (bytesRead == 0) break;
                }
                catch
                {
                    break;
                }

                // Echo the data back to the client
                await stream.WriteAsync(buffer.AsMemory(0, bytesRead));
            }
        }
    });
}