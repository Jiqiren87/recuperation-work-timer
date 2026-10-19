namespace RestWorkTimer.Core;

public enum TimerState
{
    Idle,
    Working,
    Rest
}

public enum CycleEndReason
{
    Rest,
    Lock,
    Sleep,
    ManualReset,
    Exit
}

public enum CycleStartReason
{
    HumanInput,
    AfterRest,
    Manual,
    FirstRun
}

public sealed record TimerSnapshot(
    TimerState State,
    TimeSpan Remaining,
    TimeSpan Duration,
    DateTimeOffset? CycleStartedAt,
    DateTimeOffset? RestStartedAt,
    Guid? SessionId,
    CycleStartReason? StartReason)
{
    public bool IsWorking => State == TimerState.Working;
    public bool IsRest => State == TimerState.Rest;
    public bool IsIdle => State == TimerState.Idle;
}

public sealed record WorkCycleResult(
    Guid SessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    TimeSpan Planned,
    TimeSpan Worked,
    CycleEndReason Reason,
    DateTimeOffset? RestStartedAt);

public sealed record RestResult(
    Guid SessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    TimeSpan Duration,
    string EndReason);

public sealed class WorkCycleEndedEventArgs : EventArgs
{
    public WorkCycleEndedEventArgs(WorkCycleResult result) => Result = result;
    public WorkCycleResult Result { get; }
}

public sealed class RestEndedEventArgs : EventArgs
{
    public RestEndedEventArgs(RestResult result) => Result = result;
    public RestResult Result { get; }
}

public sealed class ReminderEventArgs : EventArgs
{
    public ReminderEventArgs(int secondsRemaining) => SecondsRemaining = secondsRemaining;
    public int SecondsRemaining { get; }
}
