namespace RestWorkTimer.UI;

public sealed class RestReleaseForm : Form
{
    private readonly Label _title;
    private readonly Label _body;
    private readonly Label _countdownLabel;
    private readonly Button _release;
    private readonly System.Windows.Forms.Timer _countdownTimer;
    private readonly FlowLayoutPanel _buttonsPanel;
    private TimeSpan _minimumRemaining;
    private int _secondsLeft = 15;

    public RestReleaseForm(TimeSpan worked, TimeSpan minimumRemaining = default)
    {
        _minimumRemaining = minimumRemaining > TimeSpan.Zero
            ? minimumRemaining
            : TimeSpan.Zero;

        Text = "休息中";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        TopMost = true;
        ControlBox = false;
        MinimizeBox = false;
        MaximizeBox = false;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(500, 280);
        BackColor = Theme.Background;
        Padding = new Padding(26);

        _title = new Label
        {
            Font = Theme.Ui(16f, FontStyle.Bold),
            ForeColor = Theme.Text,
            Dock = DockStyle.Top,
            Height = 44,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _body = new Label
        {
            Font = Theme.Ui(10f),
            ForeColor = Theme.Muted,
            Dock = DockStyle.Top,
            Height = 120,
            TextAlign = ContentAlignment.TopLeft
        };

        _countdownLabel = new Label
        {
            Text = "15 秒后重新锁屏",
            Font = Theme.Ui(10f, FontStyle.Bold),
            ForeColor = Theme.Rest,
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(0, 4, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };
        _buttonsPanel = buttons;

        _release = new Button
        {
            Text = "结束休息，开始新的工作周期",
            Width = 250
        };
        Theme.StylePrimary(_release);
        _release.Click += (_, _) =>
        {
            if (_minimumRemaining > TimeSpan.Zero)
            {
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        };

        var keepResting = new Button
        {
            Text = "继续休息（立即锁屏）",
            Width = 170
        };
        Theme.StyleSecondary(keepResting);
        keepResting.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttons.Controls.Add(_release);
        buttons.Controls.Add(keepResting);

        Controls.Add(buttons);
        Controls.Add(_countdownLabel);
        Controls.Add(_body);
        Controls.Add(_title);

        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };

        _countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _countdownTimer.Tick += (_, _) =>
        {
            _secondsLeft--;
            if (_minimumRemaining > TimeSpan.Zero)
            {
                _minimumRemaining -= TimeSpan.FromSeconds(1);
                if (_minimumRemaining < TimeSpan.Zero)
                {
                    _minimumRemaining = TimeSpan.Zero;
                }
            }

            UpdateReleaseState(worked);

            if (_secondsLeft <= 0)
            {
                _countdownTimer.Stop();
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            _countdownLabel.Text = $"{_secondsLeft} 秒后重新锁屏";
        };

        UpdateReleaseState(worked);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Theme.ApplyDesignSize(this, 500, 280);
        var scale = DeviceDpi / 96f;
        if (scale > 1f)
        {
            _buttonsPanel.Height = (int)Math.Round(60 * scale);
        }

        Activate();
        BringToFront();
        _countdownTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _countdownTimer.Stop();
        _countdownTimer.Dispose();
        base.OnFormClosed(e);
    }

    private void UpdateReleaseState(TimeSpan worked)
    {
        if (_minimumRemaining > TimeSpan.Zero)
        {
            _title.Text = "休息未满 · 还需 " + FormatRemaining(_minimumRemaining);
            _body.Text = $"本次工作已完成 {FormatDuration(worked)}。\n\n" +
                         "最少休息时间未到，时间到后才能结束休息。\n" +
                         "不操作会自动重新锁屏。";
            _release.Text = "休息未满（还需 " + FormatRemaining(_minimumRemaining) + "）";
            _release.Enabled = false;
            _release.BackColor = Color.FromArgb(107, 114, 128);
            return;
        }

        _title.Text = "休息中 · 自动化仍在运行";
        _body.Text = $"本次工作已完成 {FormatDuration(worked)}。\n\n" +
                     "系统已经锁屏；解锁后必须手动确认才会开始新的工作周期。\n" +
                     "不操作会自动重新锁屏。";
        _release.Text = "结束休息，开始新的工作周期";
        _release.Enabled = true;
        _release.BackColor = Theme.Accent;
    }

    private static string FormatRemaining(TimeSpan remaining)
    {
        var totalMinutes = (int)Math.Floor(remaining.TotalMinutes);
        return $"{totalMinutes:00}:{remaining.Seconds:00}";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var totalMinutes = (int)Math.Round(duration.TotalMinutes);
        return totalMinutes >= 1 ? $"{totalMinutes} 分钟" : $"{Math.Max(1, (int)duration.TotalSeconds)} 秒";
    }
}
