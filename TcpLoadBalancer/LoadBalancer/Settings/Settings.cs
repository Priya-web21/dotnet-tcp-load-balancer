using System.ComponentModel.DataAnnotations;

namespace LoadBalancer.Settings
{
    public class Settings
    {
        [Range(1, 65535)]
        public int ListenPort { get; set; }

        [Range(1, 10000)]
        public int MaxGlobalConnections { get; set; }

        [Required]
        public string BackendSelectionMode { get; set; } = "LeastConnections";

        [Required]
        public HealthCheckSettings HealthChecks { get; set; } = new();

        [MinLength(1, ErrorMessage = "At least one backend must be configured.")]
        public List<BackendSettings> Backends { get; set; } = new();
    }

    public class HealthCheckSettings
    {
        [Range(1, 3600)]
        public int IntervalSeconds { get; set; } = 5;

        [Range(100, 60000)]
        public int TimeoutMilliseconds { get; set; } = 1000;
    }

    public class BackendSettings
    {
        [Required, MinLength(1)]
        public string Name { get; set; } = "";

        [Required, MinLength(1)]
        public string Host { get; set; } = "";

        [Range(1, 65535)]
        public int Port { get; set; }

        [Range(1, 10000)]
        public int MaxConcurrentConnections { get; set; }
    }
}