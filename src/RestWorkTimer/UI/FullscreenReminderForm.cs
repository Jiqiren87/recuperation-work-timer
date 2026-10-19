namespace RestWorkTimer.UI;

using RestWorkTimer.Platform;

/// <summary>
/// Full-screen, topmost, non-activating reminder. It is intentionally much
/// louder visually than a tray balloon so it is visible over borderless
/// full-screen apps and games.
/// </summary>
public sealed class FullscreenReminderForm : Form
{
    private readonly Color _accent;
    private readonly Color _pulseAccent;
    private readonly System.Windows.Forms.Timer _closeTimer;
    private readonly System.Windows.Forms.Timer _pulseTimer;
    private readonly System.Windows.Forms.Timer _escalationTimer;
    private readonly bool _forceForeground;
    private readonly bool _escalate;
    private readonly IntPtr _previousForeground;
    private bool _pulseOn;
    private int _escalationAttempts;

    public FullscreenReminderForm(
        int secondsRemaining,
        int autoCloseSeconds,
        bool forceForeground = false)
    {
        var urgent = secondsRemaining <= 30;
        _forceForeground = forceForeground;
        _escalate = forceForeground;
        _previousForeground = forceForeground
            ? NativeMethods.GetForegroundWindow()
            : IntPtr.Zero;
        _accent = urgent ? Color.FromArgb(220, 38, 38) : Color.FromArgb(217, 119, 6);
        _pulseAccent = urgent ? Color.FromArgb(248, 113, 113) : Color.FromArgb(245, 158, 11);
        _autoCloseSeconds = Math.Clamp(autoCloseSeconds, 4, 20);

        Text = "工作计时提醒";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = _accent;
        Padding = new Padding(10);
        Opacity = 0.97;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;
        Bounds = PrimaryScreenBounds;

        var inner = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(16, 20, 28),
            Padding = new Padding(24)
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 24));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 26));

        var center = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        center.RowStyles.Add(new RowStyle(SizeType.Percent, 18));
        center.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        center.RowStyles.Add(new RowStyle(SizeType.Percent, 24));

        var title = new Label
        {
            Text = urgent ? "距离休息还有 30 秒" : "距离休息还有 1 分钟",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Ui(24f, FontStyle.Bold),
            ForeColor = _pulseAccent,
            BackColor = Color.Transparent
        };

        var time = new Label
        {
            Text = urgent ? "00:30" : "01:00",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Mono(124f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        var hint = new Label
        {
            Text = urgent ? "即将锁屏，请立即保存当前工作" : "请保存当前工作，准备收尾",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Ui(20f, FontStyle.Bold),
            ForeColor = Color.FromArgb(229, 231, 235),
            BackColor = Color.Transparent
        };

        center.Controls.Add(title, 0, 0);
        center.Controls.Add(time, 0, 1);
        center.Controls.Add(hint, 0, 2);

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(18, 22, 30),
            Padding = new Padding(28)
        };
        card.Controls.Add(center);

        var middle = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8));
        middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 84));
        middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8));
        middle.Controls.Add(card, 1, 0);

        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 55));

        var bottomLabel = new Label
        {
            Text = $"提醒将在 {_autoCloseSeconds} 秒后自动关闭；倒计时仍会继续",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomCenter,
            Font = Theme.Ui(10f),
            ForeColor = Color.FromArgb(156, 163, 175),
            BackColor = Color.Transparent
        };

        bottom.Controls.Add(bottomLabel, 0, 0);
        bottom.SetRowSpan(bottomLabel, 2);

        root.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent }, 0, 0);
        root.Controls.Add(middle, 0, 1);
        root.Controls.Add(bottom, 0, 2);

        inner.Controls.Add(root);
        Controls.Add(inner);

        _closeTimer = new System.Windows.Forms.Timer { Interval = _autoCloseSeconds * 1000 };
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            Close();
        };

        _pulseTimer = new System.Windows.Forms.Timer { Interval = 420 };
        _pulseTimer.Tick += (_, _) =>
        {
            _pulseOn = !_pulseOn;
            BackColor = _pulseOn ? _pulseAccent : _accent;
        };

        _escalationTimer = new System.Windows.Forms.Timer { Interval = 700 };
        _escalationTimer.Tick += (_, _) =>
        {
            if (!NativeMethods.IsWindow(_previousForeground)
                || IsShellWindow(_previousForeground))
            {
                _escalationTimer.Stop();
                return;
            }

            if (NativeMethods.GetForegroundWindow() == Handle)
            {
                _escalationTimer.Stop();
                return;
            }

            if (++_escalationAttempts > 6)
            {
                _escalationTimer.Stop();
                return;
            }

            // Some exclusive-fullscreen apps reclaim the foreground instantly.
            // Minimising the previous window is the reliable way to surface the
            // warning; it is only done for the urgent 30-second reminder.
            NativeMethods.ShowWindow(_previousForeground, NativeMethods.SW_MINIMIZE);
            NativeMethods.SetForegroundWindow(Handle);
            Activate();
        };
    }

    private readonly int _autoCloseSeconds;

    private static Rectangle PrimaryScreenBounds =>
        Screen.PrimaryScreen?.Bounds ?? SystemInformation.VirtualScreen;

    protected override bool ShowWithoutActivation => !_forceForeground;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            if (!_forceForeground)
            {
                parameters.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
            }

            parameters.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
            parameters.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT (click-through)
            parameters.ExStyle |= 0x00080000; // WS_EX_LAYERED
            return parameters;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Bounds = PrimaryScreenBounds;
        TopMost = true;
        var flags = NativeMethods.SWP_NOMOVE
                    | NativeMethods.SWP_NOSIZE
                    | NativeMethods.SWP_SHOWWINDOW;
        if (!_forceForeground)
        {
            flags |= NativeMethods.SWP_NOACTIVATE;
        }

        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HWND_TOPMOST,
            0, 0, 0, 0,
            flags);

        if (_forceForeground)
        {
            if (_escalate)
            {
                // At 30 seconds, take the game out of exclusive fullscreen first;
                // this is the only reliable way for a normal desktop window to
                // become visible over some Direct3D fullscreen titles.
                NativeMethods.ShowWindow(_previousForeground, NativeMethods.SW_MINIMIZE);
            }

            ForceForeground();
            if (_escalate)
            {
                _escalationTimer.Start();
            }
        }

        _pulseTimer.Start();
        _closeTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _closeTimer.Stop();
        _pulseTimer.Stop();
        _escalationTimer.Stop();
        _closeTimer.Dispose();
        _pulseTimer.Dispose();
        _escalationTimer.Dispose();
        base.OnFormClosed(e);
    }

    private static bool IsShellWindow(IntPtr window)
    {
        var buffer = new char[128];
        var length = NativeMethods.GetClassName(window, buffer, buffer.Length);
        if (length <= 0)
        {
            return false;
        }

        var className = new string(buffer, 0, length);
        return className is "Progman"
            or "WorkerW"
            or "Shell_TrayWnd"
            or "Shell_SecondaryTrayWnd";
    }

    private void ForceForeground()
    {        var foreground = NativeMethods.GetForegroundWindow();
        var foregroundThread = foreground == IntPtr.Zero
            ? 0
            : NativeMethods.GetWindowThreadProcessId(foreground, out _);
        var currentThread = NativeMethods.GetCurrentThreadId();
        var attached = false;

        try
        {
            if (foregroundThread != 0 && foregroundThread != currentThread)
            {
                attached = NativeMethods.AttachThreadInput(
                    currentThread, foregroundThread, true);
            }

            NativeMethods.BringWindowToTop(Handle);
            NativeMethods.SetForegroundWindow(Handle);
            Activate();
        }
        finally
        {
            if (attached)
            {
                NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
            }
        }
    }
}
