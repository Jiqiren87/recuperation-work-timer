using RestWorkTimer.Core;

namespace RestWorkTimer.Tests;

internal sealed class ManualClock : IClock
{
    private long _timestamp;
    private DateTimeOffset _now;

    public ManualClock(DateTimeOffset start)
    {
        _now = start;
        _timestamp = 0;
    }

    public long Timestamp => _timestamp;

    public DateTimeOffset Now => _now;

    public TimeSpan Elapsed(long startTimestamp, long endTimestamp)
        => TimeSpan.FromMilliseconds(endTimestamp - startTimestamp);

    public void Advance(TimeSpan delta)
    {
        _timestamp += (long)delta.TotalMilliseconds;
        _now += delta;
    }
}
