using System.Diagnostics;

namespace RestWorkTimer.Core;

/// <summary>
/// Testable monotonic clock. Timestamps are only compared via <see cref="Elapsed"/>.
/// </summary>
public interface IClock
{
    long Timestamp { get; }
    DateTimeOffset Now { get; }
    TimeSpan Elapsed(long startTimestamp, long endTimestamp);
}

public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();

    private SystemClock()
    {
    }

    public long Timestamp => Stopwatch.GetTimestamp();

    public DateTimeOffset Now => DateTimeOffset.Now;

    public TimeSpan Elapsed(long startTimestamp, long endTimestamp)
        => Stopwatch.GetElapsedTime(startTimestamp, endTimestamp);
}
