using System.Text.Json;

namespace RestWorkTimer.App;

public sealed class ConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly object _gate = new();

    public ConfigStore()
        : this(AppPaths.ConfigFile)
    {
    }

    public ConfigStore(string configPath)
    {
        ConfigPath = configPath;
    }

    public string ConfigPath { get; }

    public bool Exists => File.Exists(ConfigPath);

    public AppConfig Load()
    {
        lock (_gate)
        {
            if (!File.Exists(ConfigPath))
            {
                var defaults = AppConfig.CreateDefault();
                defaults.Normalize();
                SaveCore(defaults);
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)
                             ?? AppConfig.CreateDefault();
                config.Normalize();
                return config;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                TryBackupBrokenConfig();
                AppLog.Error("config.load", ex);
                var defaults = AppConfig.CreateDefault();
                defaults.Normalize();
                SaveCore(defaults);
                return defaults;
            }
        }
    }

    public void Save(AppConfig config)
    {
        config.Normalize();
        lock (_gate)
        {
            SaveCore(config);
        }
    }

    private void SaveCore(AppConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath) ?? AppPaths.DataDirectory);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        var tempPath = ConfigPath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, ConfigPath, overwrite: true);
    }

    private void TryBackupBrokenConfig()
    {
        try
        {
            var backup = ConfigPath + $".bad-{DateTime.Now:yyyyMMdd-HHmmss}";
            File.Copy(ConfigPath, backup, overwrite: true);
        }
        catch
        {
            // Best effort only.
        }
    }
}
