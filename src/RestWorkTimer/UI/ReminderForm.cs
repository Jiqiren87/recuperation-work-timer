using System.Media;

namespace RestWorkTimer.UI;

public sealed class ReminderForm : Form
{
    private readonly Label _title;
    private readonly Label _body;
    private readonly System.Windows.Forms.Timer _closeTimer;

    public ReminderForm(int secondsRemaining, bool playSound)
    {
        Text = "工作计时提醒";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = Color.FromArgb(26, 32, 44);
        Size = new Size(380, 112);
        Padding = new Padding(18, 14, 18, 12);
        Cursor = Cursors.Hand;
        AutoScaleMode = AutoScaleMode.Dpi;

        _title = new Label
        {
            Text = secondsRemaining <= 30 ? "还有 30 秒" : "还有 1 分钟",
            ForeColor = Color.White,
            Font = Theme.Ui(12f, FontStyle.Bold),
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 28
        };

        _body = new Label
        {
            Text = secondsRemaining <= 30
                ? "即将锁屏休息，请保存当前工作。"
                : "准备收尾，30 秒后会再次提醒。",
            ForeColor = Color.FromArgb(209, 213, 219),
            Font = Theme.Ui(9.5f),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft
        };

        Controls.Add(_body);
        Controls.Add(_title);

        Click += (_, _) => Close();
        foreach (Control control in Controls)
        {
            control.Click += (_, _) => Close();
        }

        _closeTimer = new System.Windows.Forms.Timer { Interval = 8000 };
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            Close();
        };

        if (playSound)
        {
            SystemSounds.Exclamation.Play();
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
            parameters.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
            return parameters;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        Location = new Point(area.Right - Width - 18, area.Bottom - Height - 18);
        _closeTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _closeTimer.Stop();
        _closeTimer.Dispose();
        base.OnFormClosed(e);
    }
}
