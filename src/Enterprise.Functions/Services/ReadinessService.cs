using Enterprise.Functions.Abstractions;
using Enterprise.Functions.Models;
using Microsoft.Extensions.Configuration;

namespace Enterprise.Functions.Services;

public sealed class ReadinessService(IClock clock, IConfiguration configuration) : IReadinessService
{
    public Task<ReadinessReport> CheckAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dependencies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["storage"] = HasValue("AzureWebJobsStorage") ? "configured" : "missing",
            ["serviceBus"] = HasValue("ServiceBusConnection") ? "configured" : "missing",
            ["eventHub"] = HasValue("EventHubConnection") ? "configured" : "missing",
            ["cosmosDb"] = HasValue("CosmosDbConnection") ? "configured" : "missing",
            ["sql"] = HasValue("SqlConnectionString") ? "configured" : "missing"
        };

        var ready = dependencies.Values.All(value => value == "configured");
        return Task.FromResult(new ReadinessReport(ready, dependencies, clock.UtcNow));
    }

    private bool HasValue(string key)
    {
        var value = configuration[key];
        return !string.IsNullOrWhiteSpace(value) && !value.Contains("replace-me", StringComparison.OrdinalIgnoreCase);
    }
}
