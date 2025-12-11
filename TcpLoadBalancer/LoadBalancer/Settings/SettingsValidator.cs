using Microsoft.Extensions.Options;

namespace LoadBalancer.Settings
{
    public class SettingsValidator : IValidateOptions<Settings>
    {
        public ValidateOptionsResult Validate(string name, Settings settings)
        {
            // Validate backend selection mode
            var validModes = new[] { "LeastConnections", "RoundRobin" };
            if (!validModes.Contains(settings.BackendSelectionMode))
            {
                return ValidateOptionsResult.Fail(
                    $"BackendSelectionMode '{settings.BackendSelectionMode}' is invalid. Valid values: {string.Join(", ", validModes)}"
                );
            }

            // Ensure unique backend names
            var duplicateNames = settings.Backends
                .GroupBy(b => b.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames.Any())
            {
                return ValidateOptionsResult.Fail(
                    $"Duplicate backend names detected: {string.Join(", ", duplicateNames)}"
                );
            }

            return ValidateOptionsResult.Success;
        }
    }
}