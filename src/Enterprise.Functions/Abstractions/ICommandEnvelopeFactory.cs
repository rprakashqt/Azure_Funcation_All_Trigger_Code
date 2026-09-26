using Enterprise.Functions.Models;

namespace Enterprise.Functions.Abstractions;

public interface ICommandEnvelopeFactory
{
    CommandEnvelope<T> Create<T>(T payload, string source, string? correlationId = null);
}
