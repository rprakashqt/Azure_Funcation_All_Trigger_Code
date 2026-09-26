namespace Enterprise.Functions.Configuration;

public sealed class EnterpriseFunctionOptions
{
    public const string SectionName = "Enterprise";

    public string EnvironmentName { get; init; } = "local";

    public string Region { get; init; } = "local";

    public int HighValueOrderThreshold { get; init; } = 10_000;

    public int ReconciliationLookbackDays { get; init; } = 1;
}
