namespace GrumphySwarm.Core.Abstractions;

/// <summary>
/// Provides an abstraction over system time for deterministic testing and clock formatting.
/// </summary>
public interface ISystemClock
{
    DateTimeOffset Now { get; }
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// Default system clock using standard system time.
/// </summary>
public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
