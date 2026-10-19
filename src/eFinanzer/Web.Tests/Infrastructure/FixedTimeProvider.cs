namespace eFinanzer.Tests.Infrastructure;

/// <summary>
/// Time provider whose clock stands still, so tests can assert exact timestamps
/// </summary>
public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public static readonly DateTimeOffset DefaultNow = new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero);

    public FixedTimeProvider() : this(DefaultNow)
    {
    }

    public override DateTimeOffset GetUtcNow() => now;
}
