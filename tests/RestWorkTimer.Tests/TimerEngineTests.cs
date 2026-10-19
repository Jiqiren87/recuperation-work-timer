using RestWorkTimer.Core;
using Xunit;

namespace RestWorkTimer.Tests;

public sealed class TimerEngineTests
{
    private static (TimerEngine Engine, ManualClock Clock) Create(int minutes = 30)
    {
        var clock = new ManualClock(new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.FromHours(8)));
        return (new TimerEngine(clock, TimeSpan.FromMinutes(minutes), new[] { 60, 30 }), clock);
    }

    [Fact]
    public void StartsOnlyOnHumanInput()
    {
        var (engine, clock) = Create();
        Assert.Equal(TimerState.Idle, engine.State);

        engine.Tick();
        Assert.Equal(TimerState.Idle, engine.State);

        Assert.True(engine.OnHumanInput());
        Assert.Equal(TimerState.Working, engine.State);

        clock.Advance(TimeSpan.FromMinutes(1));
        engine.Tick();

        Assert.Equal(TimeSpan.FromMinutes(29), engine.Snapshot.Remaining);
    }

    [Fact]
    public void FiresRemindersAtSixtyAndThirtySecondsOnce()
    {
        var (engine, clock) = Create();
        var reminders = new List<int>();
        var restDue = 0;
        WorkCycleResult? ended = null;

        engine.ReminderDue += (_, e) => reminders.Add(e.SecondsRemaining);
        engine.RestDue += (_, _) => restDue++;
        engine.WorkCycleEnded += (_, e) => ended = e.Result;

        engine.OnHumanInput();
        clock.Advance(TimeSpan.FromMinutes(29));
        engine.Tick();
        Assert.Equal(new[] { 60 }, reminders);

        clock.Advance(TimeSpan.FromSeconds(30));
        engine.Tick();
        Assert.Equal(new[] { 60, 30 }, reminders);

        clock.Advance(TimeSpan.FromSeconds(30));
        engine.Tick();

        Assert.Equal(1, restDue);
        Assert.Equal(TimerState.Rest, engine.State);
        Assert.NotNull(ended);
        Assert.Equal(CycleEndReason.Rest, ended!.Reason);
        Assert.Equal(TimeSpan.FromMinutes(30), ended.Worked);
        Assert.Equal(expected: 2, reminders.Count);
    }

    [Fact]
    public void SystemLockClearsWorkingCycle()
    {
        var (engine, clock) = Create();
        WorkCycleResult? ended = null;
        engine.WorkCycleEnded += (_, e) => ended = e.Result;

        engine.OnHumanInput();
        clock.Advance(TimeSpan.FromMinutes(10));
        engine.OnSystemLock();

        Assert.Equal(TimerState.Idle, engine.State);
        Assert.NotNull(ended);
        Assert.Equal(CycleEndReason.Lock, ended!.Reason);
        Assert.Equal(TimeSpan.FromMinutes(10), ended.Worked);
    }

    [Fact]
    public void SuspendClearsWorkButNotRest()
    {
        var (engine, clock) = Create();
        WorkCycleResult? ended = null;
        engine.WorkCycleEnded += (_, e) => ended = e.Result;

        engine.OnHumanInput();
        clock.Advance(TimeSpan.FromMinutes(5));
        Assert.True(engine.OnSuspend());

        Assert.Equal(TimerState.Idle, engine.State);
        Assert.Equal(CycleEndReason.Sleep, ended!.Reason);

        engine.OnHumanInput();
        engine.EnterRestNow();
        Assert.Equal(TimerState.Rest, engine.State);

        clock.Advance(TimeSpan.FromMinutes(5));
        Assert.False(engine.OnSuspend());
        Assert.Equal(TimerState.Rest, engine.State);
    }

    [Fact]
    public void SystemLockDuringRestIsIgnored()
    {
        var (engine, _) = Create();
        var ends = 0;
        engine.WorkCycleEnded += (_, _) => ends++;

        engine.OnHumanInput();
        engine.EnterRestNow();
        Assert.Equal(1, ends);

        engine.OnSystemLock();
        Assert.Equal(TimerState.Rest, engine.State);
        Assert.Equal(1, ends);
    }

    [Fact]
    public void ReleaseRestStartsFreshCycleAndReportsRestDuration()
    {
        var (engine, clock) = Create();
        RestResult? rest = null;
        engine.RestEnded += (_, e) => rest = e.Result;

        engine.OnHumanInput();
        clock.Advance(TimeSpan.FromMinutes(2));
        engine.EnterRestNow();
        clock.Advance(TimeSpan.FromMinutes(7));

        var snapshot = engine.ReleaseRest("manual");

        Assert.NotNull(snapshot);
        Assert.Equal(TimerState.Working, engine.State);
        Assert.Equal(TimeSpan.FromMinutes(30), snapshot!.Remaining);
        Assert.NotNull(rest);
        Assert.Equal(TimeSpan.FromMinutes(7), rest!.Duration);
        Assert.Equal("manual", rest.EndReason);
    }

    [Fact]
    public void ScheduleChangeAppliesToNextCycle()
    {
        var (engine, _) = Create();
        engine.OnHumanInput();

        engine.UpdateSchedule(TimeSpan.FromMinutes(2), new[] { 60, 30 });
        Assert.Equal(TimeSpan.FromMinutes(30), engine.Snapshot.Duration);

        engine.ResetCurrentCycle();
        engine.StartNewCycle(CycleStartReason.Manual);

        Assert.Equal(TimeSpan.FromMinutes(2), engine.Snapshot.Duration);
        Assert.Equal(new[] { 60, 30 }, engine.ReminderSeconds);
    }

    [Fact]
    public void ReminderOutsideCycleIsFiltered()
    {
        var clock = new ManualClock(DateTimeOffset.Now);
        var engine = new TimerEngine(clock, TimeSpan.FromMinutes(1), new[] { 60, 30 });

        Assert.Equal(new[] { 30 }, engine.ReminderSeconds);
    }

    [Fact]
    public void ReminderCanBeDisabledByEmptyList()
    {
        var clock = new ManualClock(DateTimeOffset.Now);
        var engine = new TimerEngine(clock, TimeSpan.FromMinutes(30), Array.Empty<int>());
        var reminders = new List<int>();
        engine.ReminderDue += (_, e) => reminders.Add(e.SecondsRemaining);

        engine.OnHumanInput();
        clock.Advance(TimeSpan.FromMinutes(30));
        engine.Tick();

        Assert.Empty(reminders);
        Assert.Equal(TimerState.Rest, engine.State);
    }
}
