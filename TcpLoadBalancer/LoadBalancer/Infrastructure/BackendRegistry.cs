using Microsoft.Extensions.Options;
using LoadBalancer.Logging;
using LoadBalancer.Models;
using System.Collections.Immutable;

namespace LoadBalancer.Infrastructure
{
    /// <summary>
    /// Maintains the registry of backend servers for the load balancer.
    /// </summary>
    public class BackendRegistry
    {
        // Immutable list ensures thread-safe enumeration across multiple consumers.
        private ImmutableList<BackendServer> _servers;
        private readonly IApplicationLogger _logger;

        /// <summary>
        /// Initializes the backend registry from configuration settings.
        /// </summary>
        /// <param name="options">Bound configuration settings.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        public BackendRegistry(
            IOptions<Settings.Settings> options,
            IApplicationLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<BackendRegistry>();

            // Convert configured backends to a thread-safe immutable list.
            _servers = options.Value.Backends
                .Select(b => new BackendServer
                {
                    Name = b.Name,
                    Host = b.Host,
                    Port = b.Port,
                    MaxConcurrentConnections = b.MaxConcurrentConnections
                })
                .ToImmutableList();

            _logger.Information($"BackendSelectionMode: {options.Value.BackendSelectionMode}");
            _logger.Information($"BackendRegistry initialized with {_servers.Count} backend servers");
        }

        /// <summary>
        /// Returns a thread-safe snapshot of all backend servers.
        /// Consumers can enumerate safely without additional locking.
        /// </summary>
        /// <returns>Immutable list of backend servers.</returns>
        public IReadOnlyList<BackendServer> GetAllServers() => _servers;
    }
}
