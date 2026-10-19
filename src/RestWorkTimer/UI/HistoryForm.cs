using RestWorkTimer.App;

namespace RestWorkTimer.UI;

public sealed class HistoryForm : Form
{
    private readonly HistoryStore _store;
    private readonly DataGridView _grid;
    private readonly Label _summary;
    private IReadOnlyList<SessionRow> _rows = Array.Empty<SessionRow>();

    public HistoryForm(HistoryStore store)
    {
        _store = store;
        Text = "工作 / 休息记录";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(980, 600);
        MinimumSize = new Size(760, 420);
        BackColor = Theme.Background;
        Font = Theme.Ui(9.5f);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 3,
            ColumnCount = 1,
            BackColor = Theme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        _summary = new Label
        {
            Text = "正在读取本地记录…",
            Dock = DockStyle.Fill,
            Font = Theme.Ui(10f, FontStyle.Bold),
            ForeColor = Theme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(_summary, 0, 0);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            BackgroundColor = Theme.Card,
            BorderStyle = BorderStyle.None,
            GridColor = Theme.Border,
            ColumnHeadersHeight = 34,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(243, 244, 246),
                ForeColor = Theme.Text,
                Font = Theme.Ui(9f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                ForeColor = Theme.Text,
                SelectionBackColor = Color.FromArgb(219, 234, 254),
                SelectionForeColor = Theme.Text,
                Font = Theme.Ui(9f)
            },
            RowTemplate = { Height = 30 }
        };

        AddColumn("日期", nameof(SessionRow.DateText), 90);
        AddColumn("工作开始", nameof(SessionRow.WorkStartText), 80);
        AddColumn("工作结束", nameof(SessionRow.WorkEndText), 80);
        AddColumn("工作分钟", nameof(SessionRow.WorkMinutesText), 70);
        AddColumn("结束原因", nameof(SessionRow.EndReasonText), 90);
        AddColumn("休息开始", nameof(SessionRow.RestStartText), 80);
        AddColumn("休息结束", nameof(SessionRow.RestEndText), 80);
        AddColumn("休息分钟", nameof(SessionRow.RestMinutesText), 70);

        root.Controls.Add(_grid, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0),
            BackColor = Theme.Background
        };

        var close = new Button { Text = "关闭", Width = 86, DialogResult = DialogResult.Cancel };
        Theme.StyleSecondary(close);

        var export = new Button { Text = "导出 CSV", Width = 104 };
        Theme.StyleSecondary(export);
        export.Click += (_, _) => ExportCsv();

        var refresh = new Button { Text = "刷新", Width = 86 };
        Theme.StyleSecondary(refresh);
        refresh.Click += (_, _) => LoadData();

        buttons.Controls.Add(close);
        buttons.Controls.Add(export);
        buttons.Controls.Add(refresh);
        root.Controls.Add(buttons, 0, 2);

        Controls.Add(root);
        CancelButton = close;
        Shown += (_, _) => LoadData();
    }

    private void AddColumn(string header, string property, int fillWeight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
    }

    private void LoadData()
    {
        try
        {
            _rows = _store.LoadRows();
            _grid.DataSource = _rows.ToList();
            var totalWork = _rows.Sum(r => r.WorkedSeconds) / 60.0;
            var totalRest = _rows.Sum(r => r.RestSeconds ?? 0) / 60.0;
            _summary.Text = $"共 {_rows.Count} 个工作周期 · 工作 {totalWork:0.#} 分钟 · " +
                            $"休息 {_rows.Count(r => r.RestSeconds is not null)} 次 / {totalRest:0.#} 分钟";
        }
        catch (Exception ex)
        {
            AppLog.Error("history.load", ex);
            MessageBox.Show(this, "读取本地记录失败：" + ex.Message, "历史记录",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExportCsv()
    {
        if (_rows.Count == 0)
        {
            MessageBox.Show(this, "还没有可导出的记录。", "导出 CSV",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV 文件 (*.csv)|*.csv",
            FileName = $"休养工作记录-{DateTime.Now:yyyyMMdd}.csv",
            AddExtension = true,
            DefaultExt = "csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _store.ExportCsv(dialog.FileName, _rows);
            MessageBox.Show(this, "已导出：" + dialog.FileName, "导出 CSV",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            AppLog.Error("history.export", ex);
            MessageBox.Show(this, "导出失败：" + ex.Message, "导出 CSV",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
