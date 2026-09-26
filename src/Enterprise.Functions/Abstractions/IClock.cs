namespace Enterprise.Functions.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
