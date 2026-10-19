using RestWorkTimer.App;

namespace RestWorkTimer.UI;

public sealed class SettingsForm : Form
{
    private readonly NumericUpDown _minutes;
    private readonly CheckBox _warn60;
    private readonly CheckBox _warn30;
    private readonly CheckBox _sound;
    private readonly CheckBox _popup;
    private readonly CheckBox _lock;
    private readonly CheckBox _keepAwake;
    private readonly CheckBox _autoStart;
    private readonly NumericUpDown _retention;

    public SettingsForm(AppConfig current, bool autoStartEnabled)
    {
        Text = "设置";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        MaximizeBox = false;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(600, 560);
        BackColor = Theme.Background;
        Font = Theme.Ui(9.5f);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 16),
            ColumnCount = 2,
            RowCount = 12,
            BackColor = Theme.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        for (var i = 0; i < 9; i++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        }
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        var heading = new Label
        {
            Text = "计时与休息设置",
            Font = Theme.Ui(14f, FontStyle.Bold),
            ForeColor = Theme.Text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(heading, 0, 0);
        root.SetColumnSpan(heading, 2);

        _minutes = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 180,
            Value = Math.Clamp(current.WorkMinutes, 1, 180),
            Dock = DockStyle.Left,
            Width = 110,
            Font = Theme.Ui(10f)
        };
        AddRow(root, "单次工作时长（分钟）", _minutes, 1);

        _warn60 = new CheckBox
        {
            Text = "提前 60 秒提醒",
            Checked = current.WarnSeconds.Contains(60),
            AutoSize = true,
            Font = Theme.Ui(9.5f)
        };
        AddRow(root, "提醒", _warn60, 2);

        _warn30 = new CheckBox
        {
            Text = "提前 30 秒提醒",
            Checked = current.WarnSeconds.Contains(30),
            AutoSize = true,
            Font = Theme.Ui(9.5f)
        };
        AddRow(root, string.Empty, _warn30, 3);

        _sound = new CheckBox
        {
            Text = "提醒时播放声音",
            Checked = current.SoundEnabled,
            AutoSize = true
        };
        AddRow(root, "提醒方式", _sound, 4);

        _popup = new CheckBox
        {
            Text = "显示右下角弹窗",
            Checked = current.PopupEnabled,
            AutoSize = true
        };
        AddRow(root, string.Empty, _popup, 5);

        _lock = new CheckBox
        {
            Text = "休息时锁定 Windows 工作站",
            Checked = current.LockOnRest,
            AutoSize = true
        };
        AddRow(root, "休息行为", _lock, 6);

        _keepAwake = new CheckBox
        {
            Text = "休息期间阻止系统空闲睡眠",
            Checked = current.KeepAwakeDuringRest,
            AutoSize = true
        };
        AddRow(root, string.Empty, _keepAwake, 7);

        _autoStart = new CheckBox
        {
            Text = "登录 Windows 后自动启动",
            Checked = autoStartEnabled,
            AutoSize = true
        };
        AddRow(root, "启动", _autoStart, 8);

        _retention = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 3650,
            Value = Math.Clamp(current.HistoryRetentionDays, 1, 3650),
            Dock = DockStyle.Left,
            Width = 110,
            Font = Theme.Ui(10f)
        };
        AddRow(root, "本地记录保留（天）", _retention, 9);

        var note = new Label
        {
            Text = "提示：工作时长和提醒修改在下一个工作周期生效；" +
                   "需要立即应用，可在主窗口点“重置计时”。\n" +
                   "至少保留一个提醒。关闭 60 秒提醒后，只保留 30 秒提醒。",
            Font = Theme.Ui(8.5f),
            ForeColor = Theme.Muted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        root.Controls.Add(note, 0, 10);
        root.SetColumnSpan(note, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0)
        };

        var save = new Button { Text = "保存", Width = 100 };
        Theme.StylePrimary(save);
        save.Click += (_, e) => SaveSettings(e);

        var cancel = new Button { Text = "取消", Width = 86, DialogResult = DialogResult.Cancel };
        Theme.StyleSecondary(cancel);

        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 11);
        root.SetColumnSpan(buttons, 2);

        Controls.Add(root);
        AcceptButton = save;
        CancelButton = cancel;

        _minutes.ValueChanged += (_, _) => UpdateReminderAvailability();
        UpdateReminderAvailability();
    }

    public AppConfig? SavedConfig { get; private set; }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Theme.ApplyDesignSize(this, 600, 560);
    }

    private static void AddRow(TableLayoutPanel root, string labelText, Control control, int row)
    {
        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Text,
            Font = Theme.Ui(9.5f)
        };

        control.Margin = new Padding(0, 4, 0, 4);
        root.Controls.Add(label, 0, row);
        root.Controls.Add(control, 1, row);
    }

    private void UpdateReminderAvailability()
    {
        var totalSeconds = (int)_minutes.Value * 60;
        var canUse60 = totalSeconds > 60;
        var canUse30 = totalSeconds > 30;

        _warn60.Enabled = canUse60;
        _warn30.Enabled = canUse30;

        if (!canUse60)
        {
            _warn60.Checked = false;
        }

        if (!canUse30)
        {
            _warn30.Checked = false;
        }
    }

    private void SaveSettings(EventArgs e)
    {
        if (!_warn60.Checked && !_warn30.Checked)
        {
            MessageBox.Show(this, "至少保留一个提醒（60 秒或 30 秒）。", "设置",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var config = new AppConfig
        {
            WorkMinutes = (int)_minutes.Value,
            WarnSeconds = new List<int>(),
            SoundEnabled = _sound.Checked,
            PopupEnabled = _popup.Checked,
            LockOnRest = _lock.Checked,
            KeepAwakeDuringRest = _keepAwake.Checked,
            AutoStart = _autoStart.Checked,
            HistoryRetentionDays = (int)_retention.Value,
            LocalRecords = true,
            RestMode = "lock",
            RestReleaseRequiresConfirm = true
        };

        if (_warn60.Checked)
        {
            config.WarnSeconds.Add(60);
        }

        if (_warn30.Checked)
        {
            config.WarnSeconds.Add(30);
        }

        config.Normalize();
        SavedConfig = config;
        DialogResult = DialogResult.OK;
        Close();
    }
}
