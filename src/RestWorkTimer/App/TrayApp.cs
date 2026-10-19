using System.ComponentModel;
using System.Media;
using System.Runtime.InteropServices;
using RestWorkTimer.Core;
using RestWorkTimer.Platform;
using RestWorkTimer.UI;

namespace RestWorkTimer.App;

public sealed class TrayApp : ApplicationContext
{
    private readonly ConfigStore _configStore;
    private readonly TimerEngine _engine;
    private readonly HistoryStore _history;
    private readonly MainForm _main;
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu;
    private readonly Icon _idleIcon;
    private readonly Icon _workingIcon;
    private readonly Icon _restIcon;
    private readonly InputMonitor _input;
    private readonly SessionPowerMonitor? _session;
    private readonly KeepAwake _keepAwake = new();
    private readonly System.Windows.Forms.Timer _tickTimer;
    private readonly System.Windows.Forms.Timer _keepAwakeTimer;
    private readonly System.Windows.Forms.Timer _unlockDelayTimer;

    private ToolStripMenuItem _statusItem = null!;
    private ToolStripMenuItem _resetItem = null!;
    private ToolStripMenuItem _restItem = null!;
    private ToolStripMenuItem _releaseItem = null!;
    private AppConfig _config;
    private ReminderForm? _reminder;
    private RestReleaseForm? _restReleaseForm;
    private bool _restReleaseOpen;
    private bool _exiting;
    private string _todaySummary = "今日：还没有工作记录";

    public TrayApp(ConfigStore configStore, AppConfig config, bool startHidden)
    {
        _configStore = configStore;
        _config = config;
        AppPaths.EnsureDirectories();

        _history = new HistoryStore();
        _engine = new TimerEngine(SystemClock.Instance, config.WorkDuration, config.GetWarnSeconds());
        _engine.WorkCycleEnded += OnWorkCycleEnded;
        _engine.RestEnded += OnRestEnded;
        _engine.ReminderDue += OnReminderDue;
        _engine.RestDue += OnRestDue;

        _idleIcon = TrayIconFactory.Create(TimerState.Idle);
        _workingIcon = TrayIconFactory.Create(TimerState.Working);
        _restIcon = TrayIconFactory.Create(TimerState.Rest);

        _menu = BuildMenu();
        _tray = new NotifyIcon
        {
            Icon = _idleIcon,
            Text = "待开始",
            Visible = true,
            ContextMenuStrip = _menu
        };
        _tray.DoubleClick += (_, _) => ShowMain();
        _menu.Opening += (_, _) => UpdateMenu();

        _main = new MainForm(this);

        _input = new InputMonitor();
        _input.HumanInputDetected += OnHumanInput;
        if (!_input.Start())
        {
            AppLog.Warn("input.hook.failed", new { error = _input.LastError });
        }

        SessionPowerMonitor? session = null;
        try
        {
            session = new SessionPowerMonitor();
            session.SessionLocked += OnSessionLocked;
            session.SessionUnlocked += OnSessionUnlocked;
            session.SystemSuspending += OnSystemSuspending;
            session.SystemResumed += OnSystemResumed;
        }
        catch (Exception ex)
        {
            AppLog.Error("session.monitor.failed", ex);
            session = null;
        }

        _session = session;

        _tickTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _tickTimer.Tick += (_, _) =>
        {
            _engine.Tick();
            UpdateUi();
        };

        _keepAwakeTimer = new System.Windows.Forms.Timer { Interval = 30000 };
        _keepAwakeTimer.Tick += (_, _) =>
        {
            if (_engine.State == TimerState.Rest && _config.KeepAwakeDuringRest)
            {
                _keepAwake.Refresh();
            }
        };

        _unlockDelayTimer = new System.Windows.Forms.Timer { Interval = 700 };
        _unlockDelayTimer.Tick += (_, _) =>
        {
            _unlockDelayTimer.Stop();
            if (_engine.State == TimerState.Rest)
            {
                ShowRestReleaseDialog();
            }
        };

        try
        {
            AutoStart.Set(_config.AutoStart, Application.ExecutablePath);
        }
        catch (Exception ex)
        {
            AppLog.Error("autostart.apply", ex);
        }

        try
        {
            _history.PurgeOlderThan(_config.HistoryRetentionDays);
        }
        catch (Exception ex)
        {
            AppLog.Error("history.purge", ex);
        }

        RefreshTodaySummary();
        _tickTimer.Start();
        _keepAwakeTimer.Start();
        UpdateUi();

        AppLog.Info("app.start", new
        {
            version = typeof(TrayApp).Assembly.GetName().Version?.ToString(),
            workMinutes = _config.WorkMinutes,
            warnSeconds = _config.WarnSeconds,
            lockOnRest = _config.LockOnRest,
            inputHook = _input.IsRunning,
            sessionMonitor = _session is not null,
            mainWidth = _main.Width,
            mainHeight = _main.Height,
            mainClient = _main.ClientSize.ToString(),
            deviceDpi = _main.DeviceDpi,
            screen = Screen.PrimaryScreen?.Bounds.ToString()
        });

        if (!startHidden)
        {
            ShowMain();
        }

        if (!_input.IsRunning)
        {
            _tray.ShowBalloonTip(
                8000,
                "输入监听启动失败",
                "无法启用全局键鼠监听，自动开始计时可能不可用。请查看日志或以管理员身份重试。",
                ToolTipIcon.Warning);
        }

        if (_session is null)
        {
            _tray.ShowBalloonTip(
                8000,
                "锁屏/睡眠监听不可用",
                "无法注册 Windows 会话通知，锁屏或睡眠时可能无法清零倒计时。",
                ToolTipIcon.Warning);
        }
    }

    public AppConfig Config => _config;

    public TimerSnapshot Snapshot => _engine.Snapshot;

    public string TodaySummary => _todaySummary;

    public void ShowMain()
    {
        if (_main.IsDisposed)
        {
            return;
        }

        if (!_main.Visible)
        {
            _main.Show();
        }

        if (_main.WindowState == FormWindowState.Minimized)
        {
            _main.WindowState = FormWindowState.Normal;
        }

        RefreshTodaySummary();
        _main.RefreshView();
        _main.Activate();
        _main.BringToFront();
    }

    public void ResetTimer()
    {
        if (_engine.State == TimerState.Rest)
        {
            MessageBox.Show(_main, "休息中不能重置计时。请先解锁并结束休息。", "重置计时",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _engine.ResetCurrentCycle();
        _engine.StartNewCycle(CycleStartReason.Manual);
        AppLog.Info("cycle.start", new { reason = "manual" });
        RefreshTodaySummary();
        UpdateUi();
    }

    public void RequestRestNow(IWin32Window? owner)
    {
        if (_engine.State != TimerState.Working)
        {
            MessageBox.Show(owner, "当前没有进行中的工作周期。", "立即休息",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(owner,
            "立即结束本轮工作并锁屏休息？\n\n自动化程序不会被结束，系统也不会进入空闲睡眠。",
            "立即休息",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (result == DialogResult.Yes)
        {
            _engine.EnterRestNow();
            UpdateUi();
        }
    }

    public void OpenSettings(IWin32Window? owner = null)
    {
        using var form = new SettingsForm(_config, AutoStart.IsEnabled());
        var result = owner is null ? form.ShowDialog() : form.ShowDialog(owner);
        if (result == DialogResult.OK && form.SavedConfig is not null)
        {
            ApplyConfig(form.SavedConfig);
        }
    }

    public void OpenHistory(IWin32Window? owner = null)
    {
        using var form = new HistoryForm(_history);
        if (owner is null)
        {
            form.ShowDialog();
        }
        else
        {
            form.ShowDialog(owner);
        }
    }

    public void ExitApplication()
    {
        if (_exiting)
        {
            return;
        }

        if (_engine.State == TimerState.Rest)
        {
            var result = MessageBox.Show(_main,
                "当前正在休息。退出程序会解除防休眠，但不会主动解锁屏幕。\n\n仍要退出吗？",
                "退出",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        _exiting = true;
        _tickTimer.Stop();
        _keepAwakeTimer.Stop();
        _unlockDelayTimer.Stop();

        try
        {
            _engine.Shutdown();
        }
        catch (Exception ex)
        {
            AppLog.Error("engine.shutdown", ex);
        }

        _keepAwake.Dispose();
        _input.Dispose();
        _session?.Dispose();

        if (_reminder is { IsDisposed: false })
        {
            _reminder.Close();
        }

        _tray.Visible = false;
        _tray.Dispose();
        _menu.Dispose();
        _idleIcon.Dispose();
        _workingIcon.Dispose();
        _restIcon.Dispose();

        _main.AllowClose = true;
        _main.Close();
        _main.Dispose();

        ExitThread();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Font = Theme.Ui(9.5f)
        };

        _statusItem = new ToolStripMenuItem("待开始") { Enabled = false };
        _resetItem = new ToolStripMenuItem("开始计时");
        _resetItem.Click += (_, _) => ResetTimer();

        _restItem = new ToolStripMenuItem("立即休息");
        _restItem.Click += (_, _) => RequestRestNow(null);

        _releaseItem = new ToolStripMenuItem("结束休息，开始工作");
        _releaseItem.Visible = false;
        _releaseItem.Click += (_, _) => ReleaseRestFromMenu();

        var showItem = new ToolStripMenuItem("打开主窗口");
        showItem.Click += (_, _) => ShowMain();

        var settingsItem = new ToolStripMenuItem("设置…");
        settingsItem.Click += (_, _) => OpenSettings();

        var historyItem = new ToolStripMenuItem("历史记录…");
        historyItem.Click += (_, _) => OpenHistory();

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => ExitApplication();

        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(showItem);
        menu.Items.Add(_resetItem);
        menu.Items.Add(_restItem);
        menu.Items.Add(_releaseItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(historyItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private void UpdateMenu()
    {
        var state = _engine.State;
        _statusItem.Text = "状态：" + TrayText(_engine.Snapshot);
        _resetItem.Text = state == TimerState.Idle ? "开始计时" : "重置计时";
        _resetItem.Enabled = state != TimerState.Rest;
        _restItem.Enabled = state == TimerState.Working;
        _releaseItem.Visible = state == TimerState.Rest;
        _releaseItem.Enabled = state == TimerState.Rest;
    }    private void UpdateUi()
    {
        if (_exiting)
        {
            return;
        }

        var snapshot = _engine.Snapshot;
        _tray.Icon = snapshot.State switch
        {
            TimerState.Working => _workingIcon,
            TimerState.Rest => _restIcon,
            _ => _idleIcon
        };
        _tray.Text = TrayText(snapshot);
        UpdateMenu();

        if (_main.Visible)
        {
            _main.RefreshView();
        }
    }

    private string TrayText(TimerSnapshot snapshot)
    {
        return snapshot.State switch
        {
            TimerState.Working => $"工作中 {Format(snapshot.Remaining)}",
            TimerState.Rest => _config.LockOnRest ? "休息中（已锁屏）" : "休息中",
            _ => "待开始"
        };
    }

    private static string Format(TimeSpan remaining)
    {
        var totalMinutes = (int)Math.Floor(remaining.TotalMinutes);
        return $"{totalMinutes:00}:{remaining.Seconds:00}";
    }

    private void OnHumanInput(object? sender, EventArgs e)
    {
        if (_exiting)
        {
            return;
        }

        if (_engine.OnHumanInput())
        {
            AppLog.Info("cycle.start", new { reason = "human-input" });
            UpdateUi();
        }
    }

    private void OnWorkCycleEnded(object? sender, WorkCycleEndedEventArgs e)
    {
        if (_config.LocalRecords)
        {
            try
            {
                _history.AppendWork(e.Result);
            }
            catch (Exception ex)
            {
                AppLog.Error("history.append.work", ex);
            }
        }

        AppLog.Info("cycle.end", new
        {
            reason = e.Result.Reason.ToString(),
            workedSeconds = (int)e.Result.Worked.TotalSeconds
        });

        RefreshTodaySummary();
        UpdateUi();
    }

    private void OnRestEnded(object? sender, RestEndedEventArgs e)
    {
        if (_config.LocalRecords)
        {
            try
            {
                _history.AppendRest(e.Result);
            }
            catch (Exception ex)
            {
                AppLog.Error("history.append.rest", ex);
            }
        }

        _keepAwake.Disable();
        AppLog.Info("rest.end", new
        {
            reason = e.Result.EndReason,
            restSeconds = (int)e.Result.Duration.TotalSeconds
        });

        RefreshTodaySummary();
        UpdateUi();
    }

    private void OnReminderDue(object? sender, ReminderEventArgs e)
    {
        AppLog.Info("reminder", new { secondsRemaining = e.SecondsRemaining });

        if (_config.SoundEnabled)
        {
            SystemSounds.Exclamation.Play();
        }

        if (!_config.PopupEnabled)
        {
            return;
        }

        if (_reminder is { IsDisposed: false })
        {
            _reminder.Close();
        }

        _reminder = new ReminderForm(e.SecondsRemaining, playSound: false);
        _reminder.FormClosed += (_, _) => _reminder = null;
        _reminder.Show();
    }

    private void OnRestDue(object? sender, EventArgs e)
    {
        EnterRest();
    }

    private void EnterRest()
    {
        if (_engine.State != TimerState.Rest)
        {
            return;
        }

        AppLog.Info("rest.start", new { keepAwake = _config.KeepAwakeDuringRest, lockOnRest = _config.LockOnRest });

        if (_config.KeepAwakeDuringRest && !_keepAwake.Enable())
        {
            AppLog.Warn("keepawake.enable.failed");
        }

        UpdateUi();

        if (_config.LockOnRest && !NativeMethods.LockWorkStation())
        {
            var error = new Win32Exception(Marshal.GetLastWin32Error());
            AppLog.Error("lock.failed", error);
            MessageBox.Show(_main,
                "锁屏失败：" + error.Message + "\n\n程序会保持休息状态并继续防休眠，请手动按 Win+L 锁屏。",
                "锁屏失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void OnSessionLocked(object? sender, EventArgs e)
    {
        AppLog.Info("session.lock", new { state = _engine.State.ToString() });

        _unlockDelayTimer.Stop();
        if (_restReleaseForm is { IsDisposed: false })
        {
            _restReleaseForm.Close();
        }

        if (_engine.State == TimerState.Rest)
        {
            UpdateUi();
            return;
        }

        _engine.OnSystemLock();
        UpdateUi();
    }

    private void OnSessionUnlocked(object? sender, EventArgs e)
    {
        AppLog.Info("session.unlock", new { state = _engine.State.ToString() });

        // If the lock notification was missed, do not let a stale cycle continue.
        if (_engine.State == TimerState.Working)
        {
            _engine.OnSystemLock();
        }

        if (_engine.State == TimerState.Rest)
        {
            _unlockDelayTimer.Stop();
            _unlockDelayTimer.Start();
        }

        UpdateUi();
    }

    private void OnSystemSuspending(object? sender, EventArgs e)
    {
        AppLog.Info("session.suspend");
        _engine.OnSuspend();
        _keepAwake.Disable();
        UpdateUi();
    }

    private void OnSystemResumed(object? sender, EventArgs e)
    {
        AppLog.Info("session.resume");

        // PBT_APMSUSPEND can be missed (fast startup / modern standby). Treat a
        // resume while Working as a new idle state rather than resuming the old timer.
        if (_engine.State == TimerState.Working)
        {
            _engine.OnSuspend();
        }

        if (_engine.State == TimerState.Rest)
        {
            if (_config.KeepAwakeDuringRest)
            {
                _keepAwake.Enable();
            }

            // A resume can land on an unlocked desktop. Lock again so the release
            // confirmation is always shown only after a real unlock.
            if (_config.LockOnRest)
            {
                NativeMethods.LockWorkStation();
            }
        }

        UpdateUi();
    }

    private void ShowRestReleaseDialog()
    {
        if (_restReleaseOpen || _engine.State != TimerState.Rest || _exiting)
        {
            return;
        }

        _restReleaseOpen = true;
        AppLog.Info("rest.release.prompt");
        try
        {
            var snapshot = _engine.Snapshot;
            var worked = snapshot.CycleStartedAt is { } startedAt && snapshot.RestStartedAt is { } restStartedAt
                ? restStartedAt - startedAt
                : _config.WorkDuration;

            using var form = new RestReleaseForm(worked);
            _restReleaseForm = form;
            DialogResult result;
            try
            {
                result = form.ShowDialog();
            }
            finally
            {
                _restReleaseForm = null;
            }

            if (result == DialogResult.OK)
            {
                _engine.ReleaseRest("manual");
                _keepAwake.Disable();
                RefreshTodaySummary();
                UpdateUi();
                ShowMain();
            }
            else
            {
                Relock();
            }
        }
        finally
        {
            _restReleaseOpen = false;
        }
    }

    private void ReleaseRestFromMenu()
    {
        if (_engine.State != TimerState.Rest)
        {
            return;
        }

        _engine.ReleaseRest("manual");
        _keepAwake.Disable();
        RefreshTodaySummary();
        UpdateUi();
        ShowMain();
    }

    private void Relock()
    {
        if (!_config.LockOnRest)
        {
            return;
        }

        if (!NativeMethods.LockWorkStation())
        {
            var error = new Win32Exception(Marshal.GetLastWin32Error());
            AppLog.Error("lock.failed", error);
            MessageBox.Show(_main,
                "重新锁屏失败：" + error.Message + "\n\n请手动按 Win+L 锁屏。",
                "锁屏失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ApplyConfig(AppConfig config)
    {
        _config = config;
        _configStore.Save(config);
        _engine.UpdateSchedule(config.WorkDuration, config.GetWarnSeconds());

        try
        {
            _history.PurgeOlderThan(config.HistoryRetentionDays);
        }
        catch (Exception ex)
        {
            AppLog.Error("history.purge", ex);
        }

        try
        {
            AutoStart.Set(config.AutoStart, Application.ExecutablePath);
        }
        catch (Exception ex)
        {
            AppLog.Error("autostart.apply", ex);
            MessageBox.Show(_main, "开机自启设置失败：" + ex.Message, "设置",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        if (_engine.State == TimerState.Rest)
        {
            if (config.KeepAwakeDuringRest)
            {
                _keepAwake.Enable();
            }
            else
            {
                _keepAwake.Disable();
            }
        }

        RefreshTodaySummary();
        UpdateUi();
        _main.RefreshView();
    }

    private void RefreshTodaySummary()
    {
        try
        {
            var stats = _history.GetTodayStats();
            _todaySummary = stats.WorkCycles == 0
                ? "今日：还没有工作记录"
                : $"今日：工作 {stats.WorkCycles} 轮 / {stats.WorkMinutes} 分钟；" +
                  $"休息 {stats.RestCount} 次 / {stats.RestMinutes} 分钟";
        }
        catch (Exception ex)
        {
            AppLog.Error("history.today", ex);
            _todaySummary = "今日：读取记录失败";
        }
    }
}
