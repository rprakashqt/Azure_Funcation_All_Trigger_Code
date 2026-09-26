using Enterprise.Functions.Abstractions;

namespace Enterprise.Functions.Services;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
