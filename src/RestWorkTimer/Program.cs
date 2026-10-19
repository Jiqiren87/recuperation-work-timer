using RestWorkTimer.App;
using RestWorkTimer.Platform;

namespace RestWorkTimer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.ThreadException += (_, e) =>
        {
            AppLog.Error("ui.unhandled", e.Exception);
            MessageBox.Show(
                "程序遇到一个错误，计时状态已保留。\n\n" + e.Exception.Message,
                "休养期间工作计时提醒期",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                AppLog.Error("app.unhandled", exception);
            }
        };

        using var singleInstance = new SingleInstance();
        if (!singleInstance.TryAcquire())
        {
            MessageBox.Show(
                "休养期间工作计时提醒期已经在运行，请查看系统托盘。",
                "已在运行",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            AppPaths.EnsureDirectories();
            var configStore = new ConfigStore();
            var config = configStore.Load();
            var startHidden = args.Any(a =>
                string.Equals(a, "--autostart", StringComparison.OrdinalIgnoreCase));

            var app = new TrayApp(configStore, config, startHidden);
            Application.Run(app);
        }
        catch (Exception ex)
        {
            AppLog.Error("app.fatal", ex);
            MessageBox.Show(
                "启动失败：\n\n" + ex.Message + "\n\n日志目录：" + AppPaths.LogDirectory,
                "休养期间工作计时提醒期",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
