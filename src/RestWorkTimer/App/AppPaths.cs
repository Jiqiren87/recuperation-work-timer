namespace RestWorkTimer.App;

public static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RestWorkTimer");

    public static string LocalDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RestWorkTimer");

    public static string ConfigFile => Path.Combine(DataDirectory, "config.json");

    public static string SessionsFile => Path.Combine(DataDirectory, "sessions.jsonl");

    public static string LogDirectory => Path.Combine(LocalDataDirectory, "logs");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LocalDataDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}
