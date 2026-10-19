using System.Text.Json;

namespace RestWorkTimer.App;

public static class AppLog
{
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Info(string eventName, object? data = null)
        => Write("info", eventName, data, null);

    public static void Warn(string eventName, object? data = null)
        => Write("warn", eventName, data, null);

    public static void Error(string eventName, Exception exception, object? data = null)
        => Write("error", eventName, data, new
        {
            type = exception.GetType().Name,
            message = exception.Message,
            stack = exception.StackTrace
        });

    private static void Write(string level, string eventName, object? data, object? error)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(AppPaths.LogDirectory);
                var path = Path.Combine(
                    AppPaths.LogDirectory,
                    $"app-{DateTime.Now:yyyyMMdd}.jsonl");

                var line = JsonSerializer.Serialize(new
                {
                    ts = DateTimeOffset.Now,
                    level,
                    evt = eventName,
                    data,
                    error
                }, JsonOptions);

                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never break the timer or the lock screen.
        }
    }
}
