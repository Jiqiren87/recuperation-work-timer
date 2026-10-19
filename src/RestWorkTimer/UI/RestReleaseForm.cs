namespace RestWorkTimer.UI;

public sealed class RestReleaseForm : Form
{
    private readonly Label _countdownLabel;
    private readonly System.Windows.Forms.Timer _countdownTimer;
    private readonly FlowLayoutPanel _buttonsPanel;
    private int _secondsLeft = 15;

    public RestReleaseForm(TimeSpan worked)
    {
        Text = "休息中";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        TopMost = true;
        ControlBox = false;
        MinimizeBox = false;
        MaximizeBox = false;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(500, 280);
        BackColor = Theme.Background;
        Padding = new Padding(26);

        var title = new Label
        {
            Text = "休息中 · 自动化仍在运行",
            Font = Theme.Ui(16f, FontStyle.Bold),
            ForeColor = Theme.Text,
            Dock = DockStyle.Top,
            Height = 44,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var body = new Label
        {
            Text = $"本次工作已完成 {FormatDuration(worked)}。\n\n" +
                   "系统已经锁屏；解锁后必须手动确认才会开始新的工作周期。\n" +
                   "不操作会自动重新锁屏。",
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

        var release = new Button
        {
            Text = "结束休息，开始新的工作周期",
            Width = 250
        };
        Theme.StylePrimary(release);
        release.Click += (_, _) =>
        {
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

        buttons.Controls.Add(release);
        buttons.Controls.Add(keepResting);

        Controls.Add(buttons);
        Controls.Add(_countdownLabel);
        Controls.Add(body);
        Controls.Add(title);

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
            if (_secondsLeft <= 0)
            {
                _countdownTimer.Stop();
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }

            _countdownLabel.Text = $"{_secondsLeft} 秒后重新锁屏";
        };
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

    private static string FormatDuration(TimeSpan duration)
    {
        var totalMinutes = (int)Math.Round(duration.TotalMinutes);
        return totalMinutes >= 1 ? $"{totalMinutes} 分钟" : $"{Math.Max(1, (int)duration.TotalSeconds)} 秒";
    }
}
