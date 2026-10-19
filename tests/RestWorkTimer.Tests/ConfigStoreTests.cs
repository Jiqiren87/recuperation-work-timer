using RestWorkTimer.App;
using Xunit;

namespace RestWorkTimer.Tests;

public sealed class ConfigStoreTests
{
    [Fact]
    public void MissingFileCreatesNormalizedDefaults()
    {
        var dir = CreateTempDirectory();
        try
        {
            var path = Path.Combine(dir, "config.json");
            var store = new ConfigStore(path);
            var config = store.Load();

            Assert.True(File.Exists(path));
            Assert.Equal(30, config.WorkMinutes);
            Assert.Equal(new[] { 60, 30 }, config.WarnSeconds);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoadRoundTrips()
    {
        var dir = CreateTempDirectory();
        try
        {
            var path = Path.Combine(dir, "config.json");
            var store = new ConfigStore(path);
            var config = new AppConfig
            {
                WorkMinutes = 2,
                WarnSeconds = new List<int> { 60, 30 },
                SoundEnabled = false,
                AutoStart = false,
                HistoryRetentionDays = 30
            };

            store.Save(config);
            var loaded = store.Load();

            Assert.Equal(2, loaded.WorkMinutes);
            Assert.Equal(new[] { 60, 30 }, loaded.WarnSeconds);
            Assert.False(loaded.SoundEnabled);
            Assert.False(loaded.AutoStart);
            Assert.Equal(30, loaded.HistoryRetentionDays);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void InvalidValuesAreClamped()
    {
        var config = new AppConfig
        {
            WorkMinutes = 0,
            WarnSeconds = new List<int> { 9999, 60, 60 },
            HistoryRetentionDays = 0,
            RestMode = "something-else"
        };

        config.Normalize();

        Assert.Equal(1, config.WorkMinutes);
        Assert.Equal("lock", config.RestMode);
        Assert.Equal(1, config.HistoryRetentionDays);
        Assert.Equal(new[] { 30 }, config.WarnSeconds);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "RestWorkTimerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
