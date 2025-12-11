using LoadBalancer.Infrastructure;
using LoadBalancer.Logging;
using LoadBalancer.Selection;
using LoadBalancer.Services;
using LoadBalancer.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

// ---------------------------
//  Bootstrap configuration
// ---------------------------

// Load minimal config to get the environment
var bootstrapConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

// Read environment from bootstrap config
var envName = bootstrapConfig["Environment"] ?? "Production";
Console.WriteLine($"Starting Load Balancer in environment: {envName}");

// ---------------------------------------------
//  Build host with environment-specific config
// ---------------------------------------------

var builder = Host.CreateApplicationBuilder(args);
builder.Environment.EnvironmentName = envName;

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{envName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// ----------------------------------
//  Bind Settings and add validation
// ----------------------------------

builder.Services
    .Configure<Settings>(builder.Configuration.GetSection("Settings"))
    .AddSingleton<IValidateOptions<Settings>, SettingsValidator>();

// Perform fail-fast validation during startup using DataAnnotations and custom validators
var tempProvider = builder.Services.BuildServiceProvider();
try
{
    var options = tempProvider.GetRequiredService<IOptions<Settings>>().Value;
    Validator.ValidateObject(options, new ValidationContext(options), validateAllProperties: true);
}
catch (Exception ex)
{
    Console.WriteLine($"Configuration validation failed: {ex.Message}");
    throw;
}

// ---------------------------
//  Logging
// ---------------------------

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug(); // only for development/debugging
}

builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// ---------------------------
//  Dependency Injection
// ---------------------------

builder.Services.AddSingleton<IApplicationLoggerFactory, ApplicationLoggerFactory>();
builder.Services.AddSingleton<BackendRegistry>();
builder.Services.AddSingleton<ConnectionQueue>();

// Backend selector based on Settings.BackendSelectionMode
builder.Services.AddSingleton<IBackendSelector>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<Settings>>().Value;
    var registry = sp.GetRequiredService<BackendRegistry>();

    return settings.BackendSelectionMode switch
    {
        "LeastConnections" => new LeastConnectionsBackendSelector(registry),
        "RoundRobin" => new RoundRobinBackendSelector(registry),
        _ => throw new InvalidOperationException($"Unsupported BackendSelectionMode: {settings.BackendSelectionMode}")
    };
});

// ---------------------------
//  Hosted Services
// ---------------------------

builder.Services.AddHostedService<TcpListenerService>();
builder.Services.AddHostedService<DispatcherService>();
builder.Services.AddHostedService<HealthCheckService>();

// ---------------------------
//  Run the application
// ---------------------------

await builder.Build().RunAsync();
