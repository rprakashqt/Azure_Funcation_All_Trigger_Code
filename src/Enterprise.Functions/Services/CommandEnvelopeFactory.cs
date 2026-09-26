using Enterprise.Functions.Abstractions;
using Enterprise.Functions.Models;

namespace Enterprise.Functions.Services;

public sealed class CommandEnvelopeFactory(IClock clock) : ICommandEnvelopeFactory
{
    public CommandEnvelope<T> Create<T>(T payload, string source, string? correlationId = null)
    {
        return new CommandEnvelope<T>(
            Guid.NewGuid().ToString("N"),
            correlationId ?? Guid.NewGuid().ToString("N"),
            source,
            payload,
            clock.UtcNow);
    }
}
