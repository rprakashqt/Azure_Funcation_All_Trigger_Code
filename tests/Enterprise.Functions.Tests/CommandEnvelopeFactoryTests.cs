using Enterprise.Functions.Abstractions;
using Enterprise.Functions.Services;
using Xunit;

namespace Enterprise.Functions.Tests;

public sealed class CommandEnvelopeFactoryTests
{
    [Fact]
    public void Create_UsesProvidedCorrelationIdAndClock()
    {
        var now = new DateTimeOffset(2026, 9, 26, 7, 0, 0, TimeSpan.Zero);
        var factory = new CommandEnvelopeFactory(new FixedClock(now));

        var envelope = factory.Create(new { OrderId = "ORD-1" }, "unit-test", "corr-1");

        Assert.Equal("corr-1", envelope.CorrelationId);
        Assert.Equal("unit-test", envelope.Source);
        Assert.Equal(now, envelope.CreatedAtUtc);
        Assert.False(string.IsNullOrWhiteSpace(envelope.MessageId));
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
