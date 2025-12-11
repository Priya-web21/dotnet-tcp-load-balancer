# TCP Load Balancer

A simple high-performance TCP Layer 4 Load Balancer implemented in C#.
The repository contains:

* The main load balancer service
* Unit tests
* Performance benchmarks
* A **separate** echo server solution used only for manual testing

---

## Solution Structure

```
TcpLoadBalancer.sln
├── LoadBalancer                   # Main Load Balancer service
├── LoadBalancer.UnitTests         # Unit tests
└── LoadBalancer.PerformanceTests  # BenchmarkDotNet performance benchmarks

SampleEchoServer.sln               # Separate manual test server
└── SampleEchoServer               # Simple TCP echo server (single port)
```

---

# 1. LoadBalancer Project

**Purpose:**
Implements asynchronous TCP proxying with backend health checks and two routing strategies.

### Features

* Asynchronous TCP listener
* Bounded thread-safe connection queue
* Bidirectional client ↔ backend proxying
* Backend health checks
* Tracks active connections per backend
* Supports:

  * **LeastConnections**
  * **RoundRobin**

### Key Components

| Component                         | Description                                                |
| --------------------------------- | ---------------------------------------------------------- |
| `TcpListenerService`              | Accepts TCP clients and pushes them into the queue.        |
| `ConnectionQueue`                 | Bounded thread-safe channel wrapper.                       |
| `DispatcherService`               | Connects clients to selected backends and proxies traffic. |
| `TcpProxySession`                 | Handles client ↔ backend stream copying.                   |
| `BackendRegistry`                 | Stores backend metadata and tracks loads.                  |
| `HealthCheckService`              | Periodically checks backend reachability.                  |
| `LeastConnectionsBackendSelector` | Chooses backend with lowest active connections.            |
| `RoundRobinBackendSelector`       | Cycles through backends in order.                          |

### Example Configuration

```json
{
  "Settings": {
    "ListenPort": 5000,
    "MaxGlobalConnections": 100,
    "BackendSelectionMode": "LeastConnections",
    "HealthChecks": {
      "IntervalSeconds": 5,
      "TimeoutMilliseconds": 1000
    },
    "Backends": [
      { "Name": "Server1", "Host": "127.0.0.1", "Port": 6001, "MaxConcurrentConnections": 50 },
      { "Name": "Server2", "Host": "127.0.0.1", "Port": 6002, "MaxConcurrentConnections": 50 }
    ]
  }
}
```

### Run Load Balancer

```bash
dotnet run --project LoadBalancer
```

---

# 2. LoadBalancer.UnitTests

**Purpose:**
Validates backend selection logic and ensures correctness under edge cases and concurrency.

### Tests Included

* **LeastConnectionsBackendSelector**

  * Picks backend with fewest connections
  * Skips unhealthy and full backends
  * Returns null when no server is available

* **RoundRobinBackendSelector**

  * Rotates properly across all servers
  * Skips unhealthy/full servers
  * Thread-safe under heavy parallel calls

### Run Tests

```bash
dotnet test LoadBalancer.UnitTests
```

---

# 3. LoadBalancer.PerformanceTests

**Purpose:**
BenchmarkDotNet performance suite for backend selector algorithms.

### Benchmarked Methods

* `PickBackendForNextConnection()` for:

  * LeastConnectionsBackendSelector
  * RoundRobinBackendSelector

### Notes

* Must run in **Release** mode
* Results are written to:

  ```
  BenchmarkDotNet.Artifacts/results/
  ```

### Run Performance Tests

```bash
dotnet run -c Release --project LoadBalancer.PerformanceTests
```

---

# 4. SampleEchoServer (Separate Solution)

This project is **NOT part of LoadBalancer.sln**.
It exists in its **own** solution (`SampleEchoServer.sln`) and provides a lightweight TCP echo server for manual testing.

### Features

* Listens on one port (configurable)
* Echoes incoming data back to client
* To simulate multiple backends:

  * Run multiple instances manually with different ports

### Example Echo Server (simplified)

```csharp
const int port = 6001;
var listener = new TcpListener(IPAddress.Any, port);
listener.Start();

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(async () =>
    {
        using var stream = client.GetStream();
        var buffer = new byte[4096];

        while (true)
        {
            var read = await stream.ReadAsync(buffer);
            if (read == 0) break;
            await stream.WriteAsync(buffer.AsMemory(0, read));
        }
    });
}
```

### Run Echo Server

```bash
dotnet run --project SampleEchoServer
```

---

# 5. Run-All Script (Build → Test → Benchmark)

The repository includes a PowerShell script `run-all.ps1`:

```powershell
# Build solution, run unit tests, and run benchmarks in Release mode
```

### Run Script

```bash
pwsh ./run-all.ps1
```

---