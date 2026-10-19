using System.Globalization;
using System.Text;
using System.Text.Json;
using RestWorkTimer.Core;

namespace RestWorkTimer.App;

public sealed class WorkRecord
{
    public string Type { get; set; } = "work";
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public int PlannedMinutes { get; set; }
    public int WorkedSeconds { get; set; }
    public string EndReason { get; set; } = string.Empty;
    public DateTimeOffset? RestStartedAt { get; set; }
}

public sealed class RestRecord
{
    public string Type { get; set; } = "rest";
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public int RestSeconds { get; set; }
    public string EndReason { get; set; } = string.Empty;
}

public sealed class SessionRow
{
    public DateTimeOffset WorkStartedAt { get; init; }
    public DateTimeOffset WorkEndedAt { get; init; }
    public int WorkedSeconds { get; init; }
    public string EndReason { get; init; } = string.Empty;
    public DateTimeOffset? RestStartedAt { get; init; }
    public DateTimeOffset? RestEndedAt { get; init; }
    public int? RestSeconds { get; init; }
    public bool RestInProgress { get; init; }

    public string DateText => WorkStartedAt.LocalDateTime.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);
    public string WorkStartText => WorkStartedAt.LocalDateTime.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
    public string WorkEndText => WorkEndedAt.LocalDateTime.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
    public string WorkMinutesText => (WorkedSeconds / 60.0).ToString("0.#", CultureInfo.CurrentCulture);
    public string EndReasonText => HistoryStore.DescribeReason(EndReason);
    public string RestStartText => RestStartedAt?.LocalDateTime.ToString("HH:mm:ss", CultureInfo.CurrentCulture) ?? "—";
    public string RestEndText => RestEndedAt?.LocalDateTime.ToString("HH:mm:ss", CultureInfo.CurrentCulture)
                                 ?? (RestInProgress ? "进行中" : "—");
    public string RestMinutesText => RestSeconds is null
        ? (RestInProgress ? "—" : "—")
        : (RestSeconds.Value / 60.0).ToString("0.#", CultureInfo.CurrentCulture);
}

public sealed class HistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly object _gate = new();

    public HistoryStore()
        : this(AppPaths.SessionsFile)
    {
    }

    public HistoryStore(string filePath)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }

    public void AppendWork(WorkCycleResult result)
    {
        var record = new WorkRecord
        {
            Type = "work",
            Id = result.SessionId.ToString("N"),
            StartedAt = result.StartedAt,
            EndedAt = result.EndedAt,
            PlannedMinutes = (int)Math.Round(result.Planned.TotalMinutes),
            WorkedSeconds = (int)Math.Round(result.Worked.TotalSeconds),
            EndReason = result.Reason.ToString(),
            RestStartedAt = result.RestStartedAt
        };

        AppendLine(JsonSerializer.Serialize(record, JsonOptions));
    }

    public void AppendRest(RestResult result)
    {
        var record = new RestRecord
        {
            Type = "rest",
            Id = result.SessionId.ToString("N"),
            StartedAt = result.StartedAt,
            EndedAt = result.EndedAt,
            RestSeconds = (int)Math.Round(result.Duration.TotalSeconds),
            EndReason = result.EndReason
        };

        AppendLine(JsonSerializer.Serialize(record, JsonOptions));
    }

    public IReadOnlyList<SessionRow> LoadRows()
    {
        lock (_gate)
        {
            if (!File.Exists(FilePath))
            {
                return Array.Empty<SessionRow>();
            }

            var works = new Dictionary<string, WorkRecord>(StringComparer.Ordinal);
            var rests = new Dictionary<string, RestRecord>(StringComparer.Ordinal);

            foreach (var line in File.ReadLines(FilePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    using var document = JsonDocument.Parse(line);
                    if (!document.RootElement.TryGetProperty("type", out var typeElement))
                    {
                        continue;
                    }

                    var type = typeElement.GetString();
                    if (string.Equals(type, "work", StringComparison.OrdinalIgnoreCase))
                    {
                        var work = JsonSerializer.Deserialize<WorkRecord>(line, JsonOptions);
                        if (work is not null && !string.IsNullOrWhiteSpace(work.Id))
                        {
                            works[work.Id] = work;
                        }
                    }
                    else if (string.Equals(type, "rest", StringComparison.OrdinalIgnoreCase))
                    {
                        var rest = JsonSerializer.Deserialize<RestRecord>(line, JsonOptions);
                        if (rest is not null && !string.IsNullOrWhiteSpace(rest.Id))
                        {
                            rests[rest.Id] = rest;
                        }
                    }
                }
                catch (JsonException)
                {
                    // Skip a damaged line; keep the rest of the history usable.
                }
            }

            var rows = new List<SessionRow>(works.Count);
            foreach (var work in works.Values)
            {
                rests.TryGetValue(work.Id, out var rest);
                var inProgress = rest is null
                                 && string.Equals(work.EndReason, "Rest", StringComparison.OrdinalIgnoreCase);

                rows.Add(new SessionRow
                {
                    WorkStartedAt = work.StartedAt,
                    WorkEndedAt = work.EndedAt,
                    WorkedSeconds = work.WorkedSeconds,
                    EndReason = work.EndReason,
                    RestStartedAt = rest?.StartedAt ?? work.RestStartedAt,
                    RestEndedAt = rest?.EndedAt,
                    RestSeconds = rest?.RestSeconds,
                    RestInProgress = inProgress
                });
            }

            return rows
                .OrderByDescending(r => r.WorkStartedAt)
                .ToArray();
        }
    }

    public (int WorkCycles, int WorkMinutes, int RestCount, int RestMinutes) GetTodayStats()
    {
        var today = DateTime.Today;
        var rows = LoadRows()
            .Where(r => r.WorkStartedAt.LocalDateTime.Date == today)
            .ToArray();

        var workMinutes = (int)Math.Round(rows.Sum(r => r.WorkedSeconds) / 60.0);
        var restRows = rows
            .Where(r => r.RestStartedAt?.LocalDateTime.Date == today && r.RestSeconds is not null)
            .ToArray();

        return (
            rows.Length,
            workMinutes,
            restRows.Length,
            (int)Math.Round(restRows.Sum(r => r.RestSeconds ?? 0) / 60.0));
    }

    public void ExportCsv(string path, IReadOnlyList<SessionRow> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine("日期,工作开始,工作结束,工作分钟,结束原因,休息开始,休息结束,休息分钟");

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(',',
                Csv(row.DateText),
                Csv(row.WorkStartText),
                Csv(row.WorkEndText),
                Csv(row.WorkMinutesText),
                Csv(row.EndReasonText),
                Csv(row.RestStartText),
                Csv(row.RestEndText),
                Csv(row.RestMinutesText)));
        }

        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    public void PurgeOlderThan(int days)
    {
        lock (_gate)
        {
            if (!File.Exists(FilePath) || days <= 0)
            {
                return;
            }

            var cutoff = DateTimeOffset.Now.AddDays(-days);
            var keptLines = new List<string>();

            foreach (var line in File.ReadLines(FilePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    using var document = JsonDocument.Parse(line);
                    if (document.RootElement.TryGetProperty("endedAt", out var endedElement)
                        && endedElement.TryGetDateTimeOffset(out var endedAt)
                        && endedAt < cutoff)
                    {
                        continue;
                    }

                    keptLines.Add(line);
                }
                catch (JsonException)
                {
                    // Keep malformed lines; they are harmless and may be diagnosed.
                    keptLines.Add(line);
                }
            }

            if (keptLines.Count == 0)
            {
                File.WriteAllText(FilePath, string.Empty);
                return;
            }

            var tempPath = FilePath + ".tmp";
            File.WriteAllLines(tempPath, keptLines);
            File.Move(tempPath, FilePath, overwrite: true);
        }
    }

    public static string DescribeReason(string reason)
    {
        return reason switch
        {
            "Rest" => "进入休息",
            "Lock" => "锁屏清零",
            "Sleep" => "睡眠清零",
            "ManualReset" => "手动重置",
            "Exit" => "退出",
            "manual" => "手动解除",
            "exit" => "退出时解除",
            _ => reason
        };
    }

    private void AppendLine(string line)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath) ?? AppPaths.DataDirectory);
            File.AppendAllText(FilePath, line + Environment.NewLine);
        }
    }

    private static string Csv(string value)
    {
        if (value.Contains('"'))
        {
            value = value.Replace("\"", "\"\"");
        }

        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value}\""
            : value;
    }
}
