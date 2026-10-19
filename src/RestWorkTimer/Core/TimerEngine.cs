namespace RestWorkTimer.Core;

/// <summary>
/// Pure work/rest state machine. No Win32 or UI dependencies, so it can be unit tested.
/// </summary>
public sealed class TimerEngine
{
    private static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan MaxDuration = TimeSpan.FromMinutes(180);

    private readonly IClock _clock;
    private readonly object _gate = new();

    private TimeSpan _configuredDuration;
    private int[] _configuredReminders = Array.Empty<int>();

    private TimerState _state = TimerState.Idle;
    private TimeSpan _activeDuration;
    private int[] _activeReminders = Array.Empty<int>();
    private bool[] _fired = Array.Empty<bool>();
    private Guid _sessionId;
    private long _startedTimestamp;
    private DateTimeOffset _startedAt;
    private DateTimeOffset? _restStartedAt;
    private CycleStartReason? _startReason;

    public TimerEngine(IClock clock, TimeSpan duration, IEnumerable<int> reminderSeconds)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        (_configuredDuration, _configuredReminders) = Normalize(duration, reminderSeconds);
    }

    public event EventHandler<WorkCycleEndedEventArgs>? WorkCycleEnded;
    public event EventHandler<RestEndedEventArgs>? RestEnded;
    public event EventHandler<ReminderEventArgs>? ReminderDue;
    public event EventHandler? RestDue;

    public TimerState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    public TimeSpan ConfiguredDuration
    {
        get
        {
            lock (_gate)
            {
                return _configuredDuration;
            }
        }
    }

    public IReadOnlyList<int> ReminderSeconds
    {
        get
        {
            lock (_gate)
            {
                return _configuredReminders.ToArray();
            }
        }
    }

    public TimerSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                return BuildSnapshotLocked();
            }
        }
    }

    /// <summary>
    /// Applies to the next cycle. The currently running cycle keeps its original schedule.
    /// </summary>
    public void UpdateSchedule(TimeSpan duration, IEnumerable<int> reminderSeconds)
    {
        var normalized = Normalize(duration, reminderSeconds);
        lock (_gate)
        {
            _configuredDuration = normalized.Duration;
            _configuredReminders = normalized.Reminders;
        }
    }

    /// <summary>
    /// A real (non-injected) keyboard/mouse event. Starts a new cycle only while idle.
    /// </summary>
    public bool OnHumanInput()
    {
        lock (_gate)
        {
            if (_state != TimerState.Idle)
            {
                return false;
            }

            StartLocked(CycleStartReason.HumanInput);
            return true;
        }
    }

    public void StartNewCycle(CycleStartReason reason)
    {
        lock (_gate)
        {
            if (_state != TimerState.Idle)
            {
                return;
            }

            StartLocked(reason);
        }
    }

    public TimerSnapshot ResetCurrentCycle()
    {
        WorkCycleResult? ended;
        lock (_gate)
        {
            ended = _state == TimerState.Working
                ? EndWorkLocked(CycleEndReason.ManualReset)
                : null;
        }

        FireWorkEnded(ended);
        return Snapshot;
    }

    /// <summary>
    /// Called by the UI timer. Fires at most one work-end transition per call.
    /// </summary>
    public void Tick()
    {
        WorkCycleResult? ended = null;
        var restDue = false;
        var reminders = new List<int>();

        lock (_gate)
        {
            if (_state != TimerState.Working)
            {
                return;
            }

            var elapsed = _clock.Elapsed(_startedTimestamp, _clock.Timestamp);
            var remaining = _activeDuration - elapsed;

            if (remaining <= TimeSpan.Zero)
            {
                ended = EndWorkLocked(CycleEndReason.Rest);
                restDue = ended is not null;
            }
            else
            {
                for (var i = 0; i < _activeReminders.Length; i++)
                {
                    if (!_fired[i] && remaining.TotalSeconds <= _activeReminders[i])
                    {
                        _fired[i] = true;
                        reminders.Add(_activeReminders[i]);
                    }
                }
            }
        }

        FireWorkEnded(ended);
        foreach (var seconds in reminders)
        {
            ReminderDue?.Invoke(this, new ReminderEventArgs(seconds));
        }

        if (restDue)
        {
            RestDue?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// A system lock not initiated by this application resets the countdown.
    /// A lock that happens while already in Rest is ignored (it is our own lock).
    /// </summary>
    public void OnSystemLock()
    {
        WorkCycleResult? ended = null;
        lock (_gate)
        {
            if (_state == TimerState.Rest)
            {
                return;
            }

            if (_state == TimerState.Working)
            {
                ended = EndWorkLocked(CycleEndReason.Lock);
            }
            else
            {
                _state = TimerState.Idle;
            }
        }

        FireWorkEnded(ended);
    }

    /// <summary>
    /// Sleep/hibernate resets a running work cycle. An existing Rest is preserved.
    /// </summary>
    public bool OnSuspend()
    {
        WorkCycleResult? ended = null;
        var cleared = false;

        lock (_gate)
        {
            if (_state == TimerState.Rest)
            {
                return false;
            }

            if (_state == TimerState.Working)
            {
                ended = EndWorkLocked(CycleEndReason.Sleep);
                cleared = true;
            }
            else
            {
                _state = TimerState.Idle;
            }
        }

        FireWorkEnded(ended);
        return cleared;
    }

    public TimerSnapshot OnResume() => Snapshot;

    /// <summary>
    /// Manual "rest now" button. Works only while a cycle is running.
    /// </summary>
    public bool EnterRestNow()
    {
        WorkCycleResult? ended;
        lock (_gate)
        {
            ended = _state == TimerState.Working
                ? EndWorkLocked(CycleEndReason.Rest)
                : null;
        }

        FireWorkEnded(ended);
        if (ended is not null)
        {
            RestDue?.Invoke(this, EventArgs.Empty);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Ends Rest and immediately starts a fresh work cycle.
    /// </summary>
    public TimerSnapshot? ReleaseRest(string endReason)
    {
        RestResult? rest;
        lock (_gate)
        {
            if (_state != TimerState.Rest)
            {
                return null;
            }

            rest = EndRestLocked(endReason);
            StartLocked(CycleStartReason.AfterRest);
        }

        FireRestEnded(rest);
        return Snapshot;
    }

    public void Shutdown()
    {
        WorkCycleResult? work = null;
        RestResult? rest = null;

        lock (_gate)
        {
            if (_state == TimerState.Working)
            {
                work = EndWorkLocked(CycleEndReason.Exit);
            }
            else if (_state == TimerState.Rest)
            {
                rest = EndRestLocked("exit");
                _state = TimerState.Idle;
            }
        }

        FireWorkEnded(work);
        FireRestEnded(rest);
    }

    private void StartLocked(CycleStartReason reason)
    {
        _state = TimerState.Working;
        _sessionId = Guid.NewGuid();
        _startedTimestamp = _clock.Timestamp;
        _startedAt = _clock.Now;
        _restStartedAt = null;
        _startReason = reason;
        _activeDuration = _configuredDuration;
        _activeReminders = _configuredReminders.ToArray();
        _fired = new bool[_activeReminders.Length];
    }

    private WorkCycleResult? EndWorkLocked(CycleEndReason reason)
    {
        if (_state != TimerState.Working)
        {
            return null;
        }

        var endedAt = _clock.Now;
        var worked = _clock.Elapsed(_startedTimestamp, _clock.Timestamp);
        if (worked < TimeSpan.Zero)
        {
            worked = TimeSpan.Zero;
        }

        if (worked > _activeDuration)
        {
            worked = _activeDuration;
        }

        var isRest = reason == CycleEndReason.Rest;
        var result = new WorkCycleResult(
            _sessionId,
            _startedAt,
            endedAt,
            _activeDuration,
            worked,
            reason,
            isRest ? endedAt : null);

        _state = isRest ? TimerState.Rest : TimerState.Idle;
        _restStartedAt = isRest ? endedAt : null;
        return result;
    }

    private RestResult? EndRestLocked(string endReason)
    {
        if (_state != TimerState.Rest)
        {
            return null;
        }

        var endedAt = _clock.Now;
        var startedAt = _restStartedAt ?? endedAt;
        var duration = endedAt - startedAt;
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        var result = new RestResult(_sessionId, startedAt, endedAt, duration, endReason);
        _state = TimerState.Idle;
        _restStartedAt = null;
        return result;
    }

    private TimerSnapshot BuildSnapshotLocked()
    {
        var remaining = TimeSpan.Zero;
        if (_state == TimerState.Working)
        {
            remaining = _activeDuration - _clock.Elapsed(_startedTimestamp, _clock.Timestamp);
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }
        }

        var duration = _state == TimerState.Working ? _activeDuration : _configuredDuration;
        var hasCycle = _state != TimerState.Idle;

        return new TimerSnapshot(
            _state,
            remaining,
            duration,
            hasCycle ? _startedAt : null,
            _state == TimerState.Rest ? _restStartedAt : null,
            hasCycle ? _sessionId : null,
            hasCycle ? _startReason : null);
    }

    private void FireWorkEnded(WorkCycleResult? result)
    {
        if (result is not null)
        {
            WorkCycleEnded?.Invoke(this, new WorkCycleEndedEventArgs(result));
        }
    }

    private void FireRestEnded(RestResult? result)
    {
        if (result is not null)
        {
            RestEnded?.Invoke(this, new RestEndedEventArgs(result));
        }
    }

    private static (TimeSpan Duration, int[] Reminders) Normalize(
        TimeSpan duration,
        IEnumerable<int>? reminderSeconds)
    {
        if (duration < MinDuration)
        {
            duration = MinDuration;
        }
        else if (duration > MaxDuration)
        {
            duration = MaxDuration;
        }

        var totalSeconds = (int)Math.Floor(duration.TotalSeconds);
        var reminders = (reminderSeconds ?? Array.Empty<int>())
            .Where(s => s > 0 && s < totalSeconds)
            .Distinct()
            .OrderByDescending(s => s)
            .ToArray();

        return (duration, reminders);
    }
}
