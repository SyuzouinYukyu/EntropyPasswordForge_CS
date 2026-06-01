using System.Security.Cryptography;

namespace EntropyPasswordForge_CS;

internal sealed class MainForm : Form
{
    private const string AppVersion = "v1.0.8";
    private const int RequiredMouseEvents = 256;

    private readonly MouseEntropyCollector _mouseCollector = new();
    private readonly SplitContainer _logSplitContainer = new();
    private readonly Panel _entropyPanel = new();
    private readonly ProgressBar _entropyProgress = new();
    private readonly Label _eventCountLabel = new();
    private readonly NumericUpDown _lengthNumeric = new();
    private readonly NumericUpDown _countNumeric = new();
    private readonly CheckBox _lowerCheck = new();
    private readonly CheckBox _upperCheck = new();
    private readonly CheckBox _digitsCheck = new();
    private readonly CheckBox _symbolsCheck = new();
    private readonly CheckBox _ambiguousCheck = new();
    private readonly CheckBox _requireEachCheck = new();
    private readonly CheckBox _clipboardClearCheck = new();
    private readonly CheckBox _autoCopyCheck = new();
    private readonly CheckBox _resultAutoClearCheck = new();
    private readonly ComboBox _symbolModeCombo = new();
    private readonly ComboBox _customSymbolUsageCombo = new();
    private readonly TextBox _customSymbolsTextBox = new();
    private readonly Label _customSymbolsLabel = new();
    private readonly TrackBar _upperTrackBar = new();
    private readonly TrackBar _digitsTrackBar = new();
    private readonly TrackBar _symbolsTrackBar = new();
    private readonly Label _upperTrackValueLabel = new();
    private readonly Label _digitsTrackValueLabel = new();
    private readonly Label _symbolsTrackValueLabel = new();
    private readonly TextBox _resultTextBox = new();
    private readonly TextBox _logTextBox = new();
    private readonly System.Windows.Forms.Timer _clipboardTimer = new();
    private readonly System.Windows.Forms.Timer _resultClearTimer = new();
    private string? _clipboardTextToClear;
    private bool _uiReady;
    private Point? _lastEntropyPanelMovePoint;

    public MainForm()
    {
        Text = "EntropyPasswordForge_CS_v1.0.8 - OS暗号乱数 + マウス入力エントロピー + CPUジッター混合型";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 680);
        Size = new Size(1280, 760);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = SystemColors.Control;

        Icon? icon = IconHelper.LoadApplicationIcon();
        if (icon is not null)
        {
            Icon = icon;
        }

        BuildUi();
        _uiReady = true;
        WireMouseEntropyEvents();
        WireTimers();
        UpdateEntropyStatus();
        AddLog($"{AppVersion} 起動。キーボード入力は収集しません。");
    }

    private void BuildUi()
    {
        _logSplitContainer.Dock = DockStyle.Fill;
        _logSplitContainer.Orientation = Orientation.Horizontal;
        _logSplitContainer.SplitterDistance = 610;
        _logSplitContainer.SplitterWidth = 7;
        _logSplitContainer.BackColor = SystemColors.ControlLight;
        _logSplitContainer.Panel1.BackColor = SystemColors.Control;
        _logSplitContainer.Panel2.BackColor = SystemColors.Control;
        _logSplitContainer.Panel1MinSize = 540;
        _logSplitContainer.Panel2MinSize = 90;
        _logSplitContainer.Paint += SplitContainerPaint;
        Controls.Add(_logSplitContainer);

        TableLayoutPanel main = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(6, 4, 6, 4),
            BackColor = SystemColors.Control
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 394));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _logSplitContainer.Panel1.Controls.Add(main);

        TableLayoutPanel top = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(2, 0, 2, 2),
            BackColor = SystemColors.Control
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        top.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        top.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.Controls.Add(top, 0, 0);

        Label description = new()
        {
            Dock = DockStyle.Fill,
            Text = "OS暗号乱数にマウス入力エントロピーとCPUジッターを混合してパスワードを生成します。キーボード入力・外部通信・生成パスワード保存は行いません。",
            AutoSize = false,
            Font = new Font("Yu Gothic UI", 9.4f),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0),
            BackColor = SystemColors.Control,
            ForeColor = SystemColors.ControlText
        };
        top.SetColumnSpan(description, 2);
        top.Controls.Add(description, 0, 0);

        GroupBox entropyGroup = new() { Text = "エントロピー収集エリア", Dock = DockStyle.Fill, Padding = new Padding(6), BackColor = SystemColors.Control };
        entropyGroup.Controls.Add(BuildEntropyArea());
        top.Controls.Add(entropyGroup, 0, 1);

        GroupBox settingsGroup = new() { Text = "パスワード設定", Dock = DockStyle.Fill, Padding = new Padding(6), BackColor = SystemColors.Control };
        settingsGroup.Controls.Add(BuildSettingsArea());
        top.Controls.Add(settingsGroup, 1, 1);

        TableLayoutPanel resultArea = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(2, 4, 2, 2),
            BackColor = SystemColors.Control
        };
        resultArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        resultArea.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        main.Controls.Add(resultArea, 0, 1);

        _resultTextBox.Dock = DockStyle.Fill;
        _resultTextBox.Multiline = true;
        _resultTextBox.ScrollBars = ScrollBars.Both;
        _resultTextBox.WordWrap = false;
        _resultTextBox.Font = new Font(FontFamily.GenericMonospace, 10f);
        _resultTextBox.BackColor = SystemColors.Window;
        _resultTextBox.ForeColor = SystemColors.WindowText;
        resultArea.Controls.Add(_resultTextBox, 0, 0);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = SystemColors.Control
        };
        resultArea.Controls.Add(buttons, 0, 1);
        buttons.Controls.Add(CreateButton("パスワード生成", async (_, _) => await GeneratePasswordsAsync(), 180));
        buttons.Controls.Add(CreateButton("マウスエントロピー収集リセット", (_, _) => ResetEntropy()));
        buttons.Controls.Add(CreateButton("結果を全コピー", (_, _) => CopyResults()));
        buttons.Controls.Add(CreateButton("結果を削除", (_, _) => ClearResults("生成結果を削除しました。")));
        buttons.Controls.Add(CreateButton("終了", (_, _) => Close()));

        _logTextBox.Dock = DockStyle.Fill;
        _logTextBox.Multiline = true;
        _logTextBox.ReadOnly = true;
        _logTextBox.ScrollBars = ScrollBars.Vertical;
        _logTextBox.Font = new Font(FontFamily.GenericMonospace, 9.5f);
        _logTextBox.BackColor = SystemColors.Window;
        _logTextBox.ForeColor = SystemColors.WindowText;
        _logSplitContainer.Panel2.Controls.Add(_logTextBox);
    }

    private Control BuildEntropyArea()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(1),
            BackColor = SystemColors.Control
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        _entropyPanel.Dock = DockStyle.Fill;
        _entropyPanel.BackColor = SystemColors.Window;
        _entropyPanel.BorderStyle = BorderStyle.FixedSingle;
        _entropyPanel.Cursor = Cursors.Cross;
        _entropyPanel.TabStop = true;
        _entropyPanel.Paint += (_, e) =>
        {
            TextRenderer.DrawText(
                e.Graphics,
                "マウス移動で収集。左クリックで全画面収集。",
                new Font("Yu Gothic UI", 10f),
                _entropyPanel.ClientRectangle,
                Color.FromArgb(36, 50, 62),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
        };
        layout.Controls.Add(_entropyPanel, 0, 0);

        _entropyProgress.Dock = DockStyle.Fill;
        _entropyProgress.Maximum = RequiredMouseEvents;
        layout.Controls.Add(_entropyProgress, 0, 1);

        _eventCountLabel.Dock = DockStyle.Fill;
        _eventCountLabel.TextAlign = ContentAlignment.MiddleLeft;
        layout.Controls.Add(_eventCountLabel, 0, 2);
        return layout;
    }

    private Control BuildSettingsArea()
    {
        _lengthNumeric.Minimum = 16;
        _lengthNumeric.Maximum = 256;
        _lengthNumeric.Value = 16;
        _countNumeric.Minimum = 1;
        _countNumeric.Maximum = 100;
        _countNumeric.Value = 1;

        ConfigureCheck(_lowerCheck, "英小文字", true);
        ConfigureCheck(_upperCheck, "英大文字", true);
        ConfigureCheck(_digitsCheck, "数字", true);
        ConfigureCheck(_symbolsCheck, "記号", true);
        ConfigureCheck(_ambiguousCheck, "紛らわしい文字を除外", false);
        ConfigureCheck(_requireEachCheck, "各文字種を最低1文字含める", true);
        ConfigureCheck(_clipboardClearCheck, "コピー後30秒でクリップボード消去", true);
        ConfigureCheck(_autoCopyCheck, "自動的にコピーする", false);
        ConfigureCheck(_resultAutoClearCheck, "生成結果を60秒後に自動消去", true);
        _autoCopyCheck.CheckedChanged += (_, _) => UpdateAutoCopyState();
        _upperCheck.CheckedChanged += (_, _) => UpdateCompositionState();
        _digitsCheck.CheckedChanged += (_, _) => UpdateCompositionState();
        _symbolsCheck.CheckedChanged += (_, _) => UpdateSymbolInputState();

        TabControl tabs = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Point(10, 4)
        };

        TabPage basicTab = new("基本") { BackColor = SystemColors.Control, UseVisualStyleBackColor = true };
        TabPage symbolCompositionTab = new("記号・配分") { BackColor = SystemColors.Control, UseVisualStyleBackColor = true };

        basicTab.Controls.Add(BuildBasicSettingsTab());
        symbolCompositionTab.Controls.Add(BuildSymbolCompositionTab());

        tabs.TabPages.AddRange([basicTab, symbolCompositionTab]);

        UpdateSymbolInputState();
        UpdateCompositionState();
        UpdateAutoCopyState();
        return tabs;
    }

    private Control BuildBasicSettingsTab()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 8, 8, 6),
            BackColor = SystemColors.Control
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        FlowLayoutPanel numericPanel = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = SystemColors.Control
        };
        numericPanel.Controls.Add(CreateLabeledNumeric("桁数", _lengthNumeric));
        numericPanel.Controls.Add(CreateLabeledNumeric("生成個数", _countNumeric));
        layout.Controls.Add(numericPanel, 0, 0);

        FlowLayoutPanel checks = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = false,
            BackColor = SystemColors.Control
        };
        checks.Controls.AddRange([
            _lowerCheck, _upperCheck, _digitsCheck, _symbolsCheck,
            _ambiguousCheck, _requireEachCheck, _clipboardClearCheck, _autoCopyCheck, _resultAutoClearCheck
        ]);
        layout.Controls.Add(checks, 0, 1);
        return layout;
    }

    private Control BuildSymbolCompositionTab()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 6, 8, 6),
            BackColor = SystemColors.Control
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        GroupBox compositionGroup = new()
        {
            Dock = DockStyle.Fill,
            Text = "文字配分（最低使用数）",
            Padding = new Padding(8, 4, 8, 6),
            BackColor = SystemColors.Control
        };
        compositionGroup.Controls.Add(BuildCompositionArea());
        layout.Controls.Add(compositionGroup, 0, 0);

        GroupBox symbolGroup = new()
        {
            Dock = DockStyle.Fill,
            Text = "記号設定",
            Padding = new Padding(8, 6, 8, 6),
            BackColor = SystemColors.Control
        };
        symbolGroup.Controls.Add(BuildSymbolArea());
        layout.Controls.Add(symbolGroup, 0, 1);
        return layout;
    }

    private Control BuildCompositionArea()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            Padding = new Padding(2, 8, 2, 2),
            BackColor = SystemColors.Control
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        ConfigureTrackBar(_upperTrackBar, _upperTrackValueLabel, 4);
        ConfigureTrackBar(_digitsTrackBar, _digitsTrackValueLabel, 6);
        ConfigureTrackBar(_symbolsTrackBar, _symbolsTrackValueLabel, 5);

        AddTrackRow(layout, 0, "大文字", _upperTrackBar, _upperTrackValueLabel);
        AddTrackRow(layout, 1, "数字", _digitsTrackBar, _digitsTrackValueLabel);
        AddTrackRow(layout, 2, "記号", _symbolsTrackBar, _symbolsTrackValueLabel);
        return layout;
    }

    private Control BuildSymbolArea()
    {
        TableLayoutPanel symbolPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2,
            Padding = new Padding(0),
            BackColor = SystemColors.Control
        };
        symbolPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        symbolPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        symbolPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        symbolPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        symbolPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        symbolPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        _symbolModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _symbolModeCombo.Items.AddRange(["標準記号", "カスタム記号"]);
        _symbolModeCombo.SelectedIndex = 0;
        _symbolModeCombo.SelectedIndexChanged += (_, _) => UpdateSymbolInputState();

        _customSymbolUsageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _customSymbolUsageCombo.Items.AddRange(["含む", "除外"]);
        _customSymbolUsageCombo.SelectedIndex = 0;
        _customSymbolUsageCombo.SelectedIndexChanged += (_, _) => UpdateSymbolInputState();

        _customSymbolsLabel.Text = "カスタム";
        _customSymbolsLabel.Dock = DockStyle.Fill;
        _customSymbolsLabel.TextAlign = ContentAlignment.MiddleLeft;
        _customSymbolsTextBox.Text = SymbolSetHelper.DefaultCustomSymbols;
        _customSymbolsTextBox.Dock = DockStyle.Fill;
        _customSymbolsTextBox.BackColor = SystemColors.Window;
        _customSymbolsTextBox.ForeColor = SystemColors.WindowText;
        _customSymbolsTextBox.TextChanged += (_, _) => NormalizeCustomSymbolsTextBox();

        _symbolsCheck.CheckedChanged += (_, _) => UpdateSymbolInputState();

        symbolPanel.Controls.Add(new Label { Text = "記号セット", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        symbolPanel.Controls.Add(_symbolModeCombo, 1, 0);
        symbolPanel.Controls.Add(new Label { Text = "扱い", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
        symbolPanel.Controls.Add(_customSymbolUsageCombo, 3, 0);
        symbolPanel.Controls.Add(_customSymbolsLabel, 0, 1);
        symbolPanel.Controls.Add(_customSymbolsTextBox, 1, 1);
        symbolPanel.SetColumnSpan(_customSymbolsTextBox, 3);
        return symbolPanel;
    }

    private static void ConfigureTrackBar(TrackBar trackBar, Label valueLabel, int value)
    {
        trackBar.Minimum = 0;
        trackBar.Maximum = 20;
        trackBar.TickStyle = TickStyle.None;
        trackBar.SmallChange = 1;
        trackBar.LargeChange = 2;
        trackBar.Value = value;
        trackBar.Dock = DockStyle.None;
        trackBar.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        trackBar.AutoSize = false;
        trackBar.Height = 24;
        trackBar.Margin = new Padding(0, 2, 0, 2);
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.Text = value.ToString();
        valueLabel.Margin = new Padding(6, 0, 0, 0);
        trackBar.ValueChanged += (_, _) => valueLabel.Text = trackBar.Value.ToString();
    }

    private static void AddTrackRow(TableLayoutPanel layout, int row, string labelText, TrackBar trackBar, Label valueLabel)
    {
        layout.Controls.Add(new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        layout.Controls.Add(trackBar, 1, row);
        layout.Controls.Add(valueLabel, 2, row);
    }

    private static Control CreateLabeledNumeric(string labelText, NumericUpDown numeric)
    {
        TableLayoutPanel panel = new()
        {
            Width = 174,
            Height = 28,
            ColumnCount = 2,
            Margin = new Padding(0, 0, 12, 1),
            BackColor = SystemColors.Control
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        numeric.Dock = DockStyle.Fill;
        panel.Controls.Add(numeric, 1, 0);
        return panel;
    }

    private static void ConfigureCheck(CheckBox checkBox, string text, bool value)
    {
        checkBox.Text = text;
        checkBox.Checked = value;
        checkBox.AutoSize = true;
        checkBox.Margin = new Padding(0, 1, 14, 3);
        checkBox.UseVisualStyleBackColor = true;
    }

    private static Button CreateButton(string text, EventHandler handler, int? width = null)
    {
        Button button = new()
        {
            Text = text,
            AutoSize = width is null,
            Height = 30,
            Margin = new Padding(0, 5, 8, 3),
            UseVisualStyleBackColor = true
        };
        if (width.HasValue)
        {
            button.Width = width.Value;
        }

        button.Click += handler;
        return button;
    }

    private void WireMouseEntropyEvents()
    {
        _entropyPanel.MouseMove += (_, e) => CollectMouseEntropy(e, "MouseMove");
        _entropyPanel.MouseDown += (_, e) =>
        {
            CollectMouseEntropy(e, "MouseDown");
            if (e.Button == MouseButtons.Left)
            {
                StartFullScreenEntropyMode();
            }
        };
        _entropyPanel.MouseUp += (_, e) => CollectMouseEntropy(e, "MouseUp");
        _entropyPanel.MouseDoubleClick += (_, e) => CollectMouseEntropy(e, "MouseDoubleClick");
        _entropyPanel.MouseWheel += (_, e) => CollectMouseEntropy(e, "MouseWheel");
        _entropyPanel.MouseEnter += (_, _) => _entropyPanel.Focus();
    }

    private void WireTimers()
    {
        _clipboardTimer.Interval = 30_000;
        _clipboardTimer.Tick += (_, _) => ClearClipboardIfUnchanged();
        _resultClearTimer.Interval = 60_000;
        _resultClearTimer.Tick += (_, _) => ClearResults("生成結果を自動消去しました。");
    }

    private void CollectMouseEntropy(MouseEventArgs e, string kind)
    {
        if (kind == "MouseMove" && _lastEntropyPanelMovePoint == e.Location)
        {
            return;
        }

        if (kind == "MouseMove")
        {
            _lastEntropyPanelMovePoint = e.Location;
        }

        _mouseCollector.Collect(e, _entropyPanel, this, kind);
        UpdateEntropyStatus();
    }

    private void StartFullScreenEntropyMode()
    {
        AddLog($"全画面マウス収集モード開始。マウス収集イベント数={_mouseCollector.EventCount}");
        using FullScreenEntropyForm form = new(_mouseCollector, RequiredMouseEvents, Screen.FromControl(this));
        form.ShowDialog(this);
        UpdateEntropyStatus();
        AddLog($"全画面マウス収集モード終了。マウス収集イベント数={_mouseCollector.EventCount}");
    }

    private void UpdateEntropyStatus()
    {
        int value = (int)Math.Min(RequiredMouseEvents, _mouseCollector.EventCount);
        _entropyProgress.Value = value;
        _eventCountLabel.Text = $"収集イベント数: {_mouseCollector.EventCount} / 目標 {RequiredMouseEvents}";
    }

    private void UpdateSymbolInputState()
    {
        bool symbolEnabled = _symbolsCheck.Checked;
        bool customSelected = GetSymbolMode() == SymbolSetMode.Custom;
        _symbolModeCombo.Enabled = symbolEnabled;
        _customSymbolUsageCombo.Enabled = symbolEnabled && customSelected;
        _customSymbolsLabel.Enabled = symbolEnabled && customSelected;
        _customSymbolsTextBox.Enabled = symbolEnabled && customSelected;
        NormalizeCustomSymbolsTextBox();
        UpdateCompositionState();
    }

    private SymbolSetMode GetSymbolMode() => _symbolModeCombo.SelectedIndex == 1 ? SymbolSetMode.Custom : SymbolSetMode.Standard;

    private CustomSymbolUsageMode GetCustomSymbolUsageMode() => _customSymbolUsageCombo.SelectedIndex == 1 ? CustomSymbolUsageMode.Exclude : CustomSymbolUsageMode.Include;

    private void UpdateCompositionState()
    {
        _upperTrackBar.Enabled = _upperCheck.Checked;
        _upperTrackValueLabel.Enabled = _upperCheck.Checked;
        _digitsTrackBar.Enabled = _digitsCheck.Checked;
        _digitsTrackValueLabel.Enabled = _digitsCheck.Checked;
        _symbolsTrackBar.Enabled = _symbolsCheck.Checked;
        _symbolsTrackValueLabel.Enabled = _symbolsCheck.Checked;
    }

    private void NormalizeCustomSymbolsTextBox()
    {
        string normalized = SymbolSetHelper.NormalizeSymbols(_customSymbolsTextBox.Text);
        if (_customSymbolsTextBox.Text != normalized)
        {
            int selection = Math.Min(normalized.Length, _customSymbolsTextBox.SelectionStart);
            _customSymbolsTextBox.Text = normalized;
            _customSymbolsTextBox.SelectionStart = selection;
        }

        _ = GetCustomSymbolUsageMode() == CustomSymbolUsageMode.Exclude
            ? SymbolSetHelper.ExcludeFromStandard(normalized)
            : normalized;
    }

    private void ResetEntropy()
    {
        _mouseCollector.Reset();
        _lastEntropyPanelMovePoint = null;
        UpdateEntropyStatus();
        AddLog("マウスエントロピー収集をリセットしました。");
    }

    private void UpdateAutoCopyState()
    {
        if (_autoCopyCheck.Checked)
        {
            _countNumeric.Value = 1;
            _countNumeric.Enabled = false;
            if (_uiReady)
            {
                AddLog("自動コピー=True。生成個数を1に固定しました。");
            }
        }
        else
        {
            _countNumeric.Enabled = true;
            if (_uiReady)
            {
                AddLog("自動コピー=False。生成個数の変更を有効化しました。");
            }
        }
    }

    private async Task GeneratePasswordsAsync()
    {
        ToggleUi(false);

        byte[]? mousePool = null;
        byte[]? cpuJitter = null;
        byte[]? seed = null;

        try
        {
            SymbolSetMode symbolMode = GetSymbolMode();
            CustomSymbolUsageMode customUsageMode = GetCustomSymbolUsageMode();
            string normalizedCustomSymbols = SymbolSetHelper.NormalizeSymbols(_customSymbolsTextBox.Text);
            string symbols = ResolveSymbols(symbolMode, customUsageMode, normalizedCustomSymbols);
            if (_symbolsCheck.Checked && symbols.Length == 0)
            {
                MessageBox.Show(this, "記号チェックがONですが、使用可能な記号セットが空です。設定を見直してください。", "生成エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AddLog("パスワード生成を中止しました。記号セットが空です。");
                return;
            }

            bool customInclude = _symbolsCheck.Checked && symbolMode == SymbolSetMode.Custom && customUsageMode == CustomSymbolUsageMode.Include;
            if (customInclude && symbols.Length > (int)_lengthNumeric.Value)
            {
                MessageBox.Show(this, "カスタム記号の種類数がパスワード桁数を超えています。桁数を増やすか、カスタム記号を減らしてください。", "生成エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                AddLog($"パスワード生成を中止しました。カスタム記号数が桁数を超過。有効記号数={symbols.Length}");
                return;
            }

            PasswordOptions options = new(
                (int)_lengthNumeric.Value,
                (int)_countNumeric.Value,
                _lowerCheck.Checked,
                _upperCheck.Checked,
                _digitsCheck.Checked,
                _symbolsCheck.Checked,
                symbols,
                customInclude,
                _ambiguousCheck.Checked,
                _requireEachCheck.Checked,
                _upperCheck.Checked ? _upperTrackBar.Value : 0,
                _digitsCheck.Checked ? _digitsTrackBar.Value : 0,
                _symbolsCheck.Checked ? _symbolsTrackBar.Value : 0);

            string customInfo = BuildCustomSymbolLog(symbolMode, customUsageMode, normalizedCustomSymbols.Length, symbols.Length);
            AddLog($"パスワード生成開始。マウス収集イベント数={_mouseCollector.EventCount}, 生成個数={options.Count}, 桁数={options.Length}, 英小文字={options.UseLower}, 英大文字={options.UseUpper}, 数字={options.UseDigits}, 記号={options.UseSymbols}, 大文字最低={options.MinimumUpper}, 数字最低={options.MinimumDigits}, 記号最低={options.MinimumSymbols}, 記号モード={SymbolSetHelper.GetDisplayName(symbolMode)}{customInfo}");

            mousePool = _mouseCollector.SnapshotPool();
            cpuJitter = await Task.Run(() => CpuJitterCollector.Collect(TimeSpan.FromMilliseconds(180)));
            seed = SecureMixer.BuildFinalSeed(mousePool, cpuJitter);
            List<string> passwords = PasswordGenerator.Generate(options, seed);
            if (_symbolsCheck.Checked && symbolMode == SymbolSetMode.Custom && customUsageMode == CustomSymbolUsageMode.Exclude)
            {
                EnsureExcludedSymbolsAreAbsent(passwords, normalizedCustomSymbols);
            }

            _resultTextBox.Text = string.Join(Environment.NewLine, passwords);
            AddLog($"パスワード生成完了。マウス収集イベント数={_mouseCollector.EventCount}, 生成個数={passwords.Count}");

            if (_autoCopyCheck.Checked && passwords.Count == 1)
            {
                CopyTextToClipboard(passwords[0], "生成結果を自動コピーしました。");
            }

            _resultClearTimer.Stop();
            if (_resultAutoClearCheck.Checked)
            {
                _resultClearTimer.Start();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "生成エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            AddLog("パスワード生成に失敗しました。");
        }
        finally
        {
            if (mousePool is not null) CryptographicOperations.ZeroMemory(mousePool);
            if (cpuJitter is not null) CryptographicOperations.ZeroMemory(cpuJitter);
            if (seed is not null) CryptographicOperations.ZeroMemory(seed);
            ToggleUi(true);
        }
    }

    private void CopyResults()
    {
        if (string.IsNullOrWhiteSpace(_resultTextBox.Text))
        {
            AddLog("コピー対象の生成結果がありません。");
            return;
        }

        CopyTextToClipboard(_resultTextBox.Text, $"生成結果をクリップボードへコピーしました。マウス収集イベント数={_mouseCollector.EventCount}");
    }

    private static string ResolveSymbols(SymbolSetMode symbolMode, CustomSymbolUsageMode usageMode, string normalizedCustomSymbols)
    {
        if (symbolMode == SymbolSetMode.Standard)
        {
            return SymbolSetHelper.StandardSymbols;
        }

        return usageMode == CustomSymbolUsageMode.Exclude
            ? SymbolSetHelper.ExcludeFromStandard(normalizedCustomSymbols)
            : normalizedCustomSymbols;
    }

    private static string BuildCustomSymbolLog(SymbolSetMode symbolMode, CustomSymbolUsageMode usageMode, int customCount, int usableCount)
    {
        if (symbolMode != SymbolSetMode.Custom)
        {
            return string.Empty;
        }

        return usageMode == CustomSymbolUsageMode.Exclude
            ? $", カスタム記号除外モード=True, 有効除外数={customCount}, 使用可能記号数={usableCount}"
            : $", カスタム記号含むモード=True, 有効記号数={customCount}";
    }

    private static void EnsureExcludedSymbolsAreAbsent(IEnumerable<string> passwords, string excludedSymbols)
    {
        if (excludedSymbols.Length == 0)
        {
            return;
        }

        foreach (string password in passwords)
        {
            if (password.Any(c => excludedSymbols.Contains(c, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException("除外指定された記号が生成結果に含まれました。設定を見直して再生成してください。");
            }
        }
    }

    private void CopyTextToClipboard(string text, string successLog)
    {
        try
        {
            Clipboard.SetText(text);
            _clipboardTextToClear = text;
            _clipboardTimer.Stop();
            if (_clipboardClearCheck.Checked)
            {
                _clipboardTimer.Start();
            }

            AddLog(successLog);
        }
        catch (Exception ex)
        {
            AddLog("自動コピーまたはクリップボード操作に失敗しました。");
            MessageBox.Show(this, ex.Message, "クリップボードエラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ClearClipboardIfUnchanged()
    {
        _clipboardTimer.Stop();
        if (_clipboardTextToClear is null)
        {
            return;
        }

        try
        {
            if (Clipboard.ContainsText() && Clipboard.GetText() == _clipboardTextToClear)
            {
                Clipboard.Clear();
                AddLog("クリップボードを自動消去しました。");
            }
        }
        catch
        {
            AddLog("クリップボード自動消去に失敗しました。");
        }
        finally
        {
            _clipboardTextToClear = null;
        }
    }

    private void ClearResults(string logMessage)
    {
        _resultClearTimer.Stop();
        _resultTextBox.Clear();
        AddLog(logMessage);
    }

    private void ToggleUi(bool enabled)
    {
        foreach (Control control in _logSplitContainer.Panel1.Controls)
        {
            control.Enabled = enabled;
        }
    }

    private void AddLog(string message)
    {
        string line = $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}";
        _logTextBox.AppendText(line);
    }

    private void SplitContainerPaint(object? sender, PaintEventArgs e)
    {
        if (sender is not SplitContainer split)
        {
            return;
        }

        Rectangle splitter = split.Orientation == Orientation.Horizontal
            ? new Rectangle(0, split.SplitterDistance, split.Width, split.SplitterWidth)
            : new Rectangle(split.SplitterDistance, 0, split.SplitterWidth, split.Height);

        using SolidBrush brush = new(SystemColors.ControlLight);
        e.Graphics.FillRectangle(brush, splitter);
        using Pen pen = new(SystemColors.ControlDark);
        int y = splitter.Top + splitter.Height / 2;
        e.Graphics.DrawLine(pen, 12, y, Math.Max(12, split.Width - 12), y);
    }
}
