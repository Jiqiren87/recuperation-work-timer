using RestWorkTimer.App;
using RestWorkTimer.Core;
using Xunit;

namespace RestWorkTimer.Tests;

public sealed class HistoryStoreTests
{
    [Fact]
    public void WorkAndRestRecordsJoinIntoOneRow()
    {
        var dir = CreateTempDirectory();
        try
        {
            var path = Path.Combine(dir, "sessions.jsonl");
            var store = new HistoryStore(path);
            var start = new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.FromHours(8));
            var workEnd = start.AddMinutes(30);
            var restEnd = workEnd.AddMinutes(8);
            var id = Guid.NewGuid();

            store.AppendWork(new WorkCycleResult(
                id, start, workEnd, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30),
                CycleEndReason.Rest, workEnd));
            store.AppendRest(new RestResult(
                id, workEnd, restEnd, TimeSpan.FromMinutes(8), "manual"));

            var rows = store.LoadRows();

            var row = Assert.Single(rows);
            Assert.Equal(30d, double.Parse(row.WorkMinutesText), 3);
            Assert.Equal("进入休息", row.EndReasonText);
            Assert.Equal("8", row.RestMinutesText);
            Assert.False(row.RestInProgress);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void RestWithoutRestRecordShowsInProgress()
    {
        var dir = CreateTempDirectory();
        try
        {
            var path = Path.Combine(dir, "sessions.jsonl");
            var store = new HistoryStore(path);
            var start = new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.FromHours(8));
            var end = start.AddMinutes(30);

            store.AppendWork(new WorkCycleResult(
                Guid.NewGuid(), start, end, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30),
                CycleEndReason.Rest, end));

            var row = Assert.Single(store.LoadRows());
            Assert.True(row.RestInProgress);
            Assert.Equal("进行中", row.RestEndText);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ExportCsvWritesHeaderAndRow()
    {
        var dir = CreateTempDirectory();
        try
        {
            var store = new HistoryStore(Path.Combine(dir, "sessions.jsonl"));
            var start = new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.FromHours(8));
            var end = start.AddMinutes(30);
            store.AppendWork(new WorkCycleResult(
                Guid.NewGuid(), start, end, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30),
                CycleEndReason.Rest, end));

            var csv = Path.Combine(dir, "out.csv");
            store.ExportCsv(csv, store.LoadRows());
            var text = File.ReadAllText(csv);

            Assert.Contains("日期,工作开始", text);
            Assert.Contains("进入休息", text);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "RestWorkTimerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
