namespace RestWorkTimer.App;

public sealed class AppConfig
{
    public int SchemaVersion { get; set; } = 1;
    public int WorkMinutes { get; set; } = 30;
    public List<int> WarnSeconds { get; set; } = new() { 60, 30 };
    public int IdleResetMinutes { get; set; } = 0;
    public bool LockOnRest { get; set; } = true;
    public bool KeepAwakeDuringRest { get; set; } = true;
    public string RestMode { get; set; } = "lock";
    public bool RestReleaseRequiresConfirm { get; set; } = true;
    public bool AutoStart { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public bool PopupEnabled { get; set; } = true;
    public bool BreakThroughFullscreen { get; set; } = true;
    public bool LocalRecords { get; set; } = true;
    public int HistoryRetentionDays { get; set; } = 180;

    public TimeSpan WorkDuration => TimeSpan.FromMinutes(WorkMinutes);

    public IReadOnlyList<int> GetWarnSeconds()
        => WarnSeconds
            .Where(s => s > 0 && s < WorkMinutes * 60)
            .Distinct()
            .OrderByDescending(s => s)
            .ToArray();

    public void Normalize()
    {
        WorkMinutes = Math.Clamp(WorkMinutes, 1, 180);
        WarnSeconds ??= new List<int>();

        var maxSeconds = WorkMinutes * 60 - 1;
        WarnSeconds = WarnSeconds
            .Where(s => s > 0 && s <= maxSeconds)
            .Distinct()
            .OrderByDescending(s => s)
            .ToList();

        if (WarnSeconds.Count == 0)
        {
            WarnSeconds.Add(30);
        }

        HistoryRetentionDays = Math.Clamp(HistoryRetentionDays, 1, 3650);
        IdleResetMinutes = Math.Clamp(IdleResetMinutes, 0, 240);

        if (!string.Equals(RestMode, "lock", StringComparison.OrdinalIgnoreCase))
        {
            RestMode = "lock";
        }
    }

    public AppConfig Clone()
    {
        var clone = (AppConfig)MemberwiseClone();
        clone.WarnSeconds = (WarnSeconds ?? new List<int>()).ToList();
        return clone;
    }

    public static AppConfig CreateDefault() => new();
}
