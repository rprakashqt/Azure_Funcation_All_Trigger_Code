using Enterprise.Functions.Models;

namespace Enterprise.Functions.Abstractions;

public interface IReadinessService
{
    Task<ReadinessReport> CheckAsync(CancellationToken cancellationToken);
}
