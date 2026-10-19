using RestWorkTimer.App;
using RestWorkTimer.Core;

namespace RestWorkTimer.UI;

public sealed class MainForm : Form
{
    private readonly TrayApp _app;
    private readonly Label _timeLabel;
    private readonly Label _stateLabel;
    private readonly Label _statsLabel;
    private readonly Button _resetButton;
    private readonly Button _restButton;
    private readonly Label _subtitleLabel;

    public MainForm(TrayApp app)
    {
        _app = app;
        Text = "休养期间工作计时提醒期";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimizeBox = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(620, 470);
        BackColor = Theme.Background;
        Font = Theme.Ui(9.5f);

        try
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            // The generated tray icon is still available.
        }

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Padding = new Padding(28, 22, 28, 20),
            ColumnCount = 1,
            RowCount = 5
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
        var title = new Label
        {
            Text = "休养期间工作计时提醒期",
            Font = Theme.Ui(16f, FontStyle.Bold),
            ForeColor = Theme.Text,
            AutoSize = true,
            Location = new Point(0, 2)
        };
        _subtitleLabel = new Label
        {
            Text = "工作时长 30 分钟 · 60 / 30 秒提醒",
            Font = Theme.Ui(9f),
            ForeColor = Theme.Muted,
            AutoSize = true,
            Location = new Point(1, 32)
        };
        header.Controls.Add(title);
        header.Controls.Add(_subtitleLabel);

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Card,
            Padding = new Padding(20, 18, 20, 16),
            Margin = new Padding(0, 4, 0, 10)
        };

        _timeLabel = new Label
        {
            Text = "--:--",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Mono(58f, FontStyle.Bold),
            ForeColor = Theme.Idle,
            AutoSize = false
        };

        _stateLabel = new Label
        {
            Text = "待开始 · 敲击键鼠开始计时",
            Dock = DockStyle.Bottom,
            Height = 40,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = Theme.Ui(11f),
            ForeColor = Theme.Muted
        };

        card.Controls.Add(_timeLabel);
        card.Controls.Add(_stateLabel);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Theme.Background
        };

        _resetButton = new Button { Text = "开始计时", Width = 112 };
        Theme.StylePrimary(_resetButton);
        _resetButton.Click += (_, _) => _app.ResetTimer();

        _restButton = new Button { Text = "立即休息", Width = 104 };
        Theme.StyleSecondary(_restButton);
        _restButton.Click += (_, _) => _app.RequestRestNow(this);

        var settingsButton = new Button { Text = "设置", Width = 88 };
        Theme.StyleSecondary(settingsButton);
        settingsButton.Click += (_, _) => _app.OpenSettings(this);

        var historyButton = new Button { Text = "历史记录", Width = 104 };
        Theme.StyleSecondary(historyButton);
        historyButton.Click += (_, _) => _app.OpenHistory(this);

        buttons.Controls.Add(_resetButton);
        buttons.Controls.Add(_restButton);
        buttons.Controls.Add(settingsButton);
        buttons.Controls.Add(historyButton);

        _statsLabel = new Label
        {
            Text = "今日：还没有工作记录",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Theme.Ui(9f),
            ForeColor = Theme.Muted,
            AutoEllipsis = true
        };

        var hint = new Label
        {
            Text = "锁屏后自动化继续运行；解锁后需手动确认解除休息。",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = Theme.Ui(8.5f),
            ForeColor = Theme.Muted
        };

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(card, 0, 1);
        root.Controls.Add(buttons, 0, 2);
        root.Controls.Add(_statsLabel, 0, 3);
        root.Controls.Add(hint, 0, 4);

        Controls.Add(root);
        RefreshView();
    }

    public bool AllowClose { get; set; }

    public void RefreshView()
    {
        if (IsDisposed)
        {
            return;
        }

        var snapshot = _app.Snapshot;
        var minutes = Math.Max(1, (int)Math.Round(_app.Config.WorkDuration.TotalMinutes));
        var currentMinutes = Math.Max(1, (int)Math.Round(snapshot.Duration.TotalMinutes));
        _subtitleLabel.Text = $"工作时长 {minutes} 分钟 · {FormatReminders(_app.Config.GetWarnSeconds())}";

        switch (snapshot.State)
        {
            case TimerState.Working:
                _timeLabel.Text = FormatRemaining(snapshot.Remaining);
                _timeLabel.ForeColor = Theme.Working;
                _stateLabel.Text = $"工作中 · 本轮 {currentMinutes} 分钟";
                _resetButton.Text = "重置计时";
                _resetButton.Enabled = true;
                _restButton.Enabled = true;
                break;

            case TimerState.Rest:
                _timeLabel.Text = "00:00";
                _timeLabel.ForeColor = Theme.Rest;
                _stateLabel.Text = "休息中 · 已锁屏，自动化仍在运行";
                _resetButton.Text = "休息中";
                _resetButton.Enabled = false;
                _restButton.Enabled = false;
                break;

            default:
                _timeLabel.Text = "--:--";
                _timeLabel.ForeColor = Theme.Idle;
                _stateLabel.Text = "待开始 · 敲击键鼠开始计时";
                _resetButton.Text = "开始计时";
                _resetButton.Enabled = true;
                _restButton.Enabled = false;
                break;
        }

        _statsLabel.Text = _app.TodaySummary;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!AllowClose && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnFormClosing(e);
    }

    private static string FormatRemaining(TimeSpan remaining)
    {
        var totalMinutes = (int)Math.Floor(remaining.TotalMinutes);
        return $"{totalMinutes:00}:{remaining.Seconds:00}";
    }

    private static string FormatReminders(IReadOnlyList<int> seconds)
    {
        if (seconds.Count == 0)
        {
            return "无提醒";
        }

        return string.Join(" / ", seconds.Select(s => $"{s} 秒")) + "提醒";
    }
}
