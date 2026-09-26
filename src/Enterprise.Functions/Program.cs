using Enterprise.Functions.Abstractions;
using Enterprise.Functions.Configuration;
using Enterprise.Functions.Middleware;
using Enterprise.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        worker.UseMiddleware<ExceptionHandlingMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.Configure<EnterpriseFunctionOptions>(context.Configuration.GetSection(EnterpriseFunctionOptions.SectionName));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IOrderWorkflowService, OrderWorkflowService>();
        services.AddSingleton<IReadinessService, ReadinessService>();
        services.AddSingleton<ICommandEnvelopeFactory, CommandEnvelopeFactory>();
        services.AddHealthChecks();
    })
    .ConfigureLogging(logging =>
    {
        logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            var applicationInsightsRule = options.Rules.FirstOrDefault(rule =>
                rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

            if (applicationInsightsRule is not null)
            {
                options.Rules.Remove(applicationInsightsRule);
            }
        });
    })
    .Build();

await host.RunAsync();
