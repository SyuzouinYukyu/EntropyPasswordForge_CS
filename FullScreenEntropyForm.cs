namespace EntropyPasswordForge_CS;

using System.Diagnostics;

internal sealed class FullScreenEntropyForm : Form, IMessageFilter
{
    private const int WmMouseMove = 0x0200;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmLButtonDblClk = 0x0203;
    private const int WmRButtonDown = 0x0204;
    private const int WmRButtonUp = 0x0205;
    private const int WmRButtonDblClk = 0x0206;
    private const int WmMButtonDown = 0x0207;
    private const int WmMButtonUp = 0x0208;
    private const int WmMButtonDblClk = 0x0209;
    private const int WmMouseWheel = 0x020A;

    private readonly MouseEntropyCollector _collector;
    private readonly int _targetEvents;
    private readonly DateTimeOffset _openedAt = DateTimeOffset.UtcNow;
    private readonly Label _instructionLabel = new();
    private readonly Label _countLabel = new();
    private readonly ProgressBar _progress = new();
    private bool _messageFilterAdded;
    private FullScreenMouseSignature? _lastAcceptedEvent;
    private Point? _lastAcceptedMoveScreenPoint;

    public FullScreenEntropyForm(MouseEntropyCollector collector, int targetEvents, Screen screen)
    {
        _collector = collector;
        _targetEvents = targetEvents;

        Text = "EntropyPasswordForge_CS_v1.0.11 全画面マウス収集";
        StartPosition = FormStartPosition.Manual;
        Bounds = screen.Bounds;
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(10, 24, 43);
        ForeColor = Color.White;
        Cursor = Cursors.Cross;
        ShowInTaskbar = false;

        Icon? icon = IconHelper.LoadApplicationIcon();
        if (icon is not null)
        {
            Icon = icon;
        }

        BuildUi();
        WireMouseEventsRecursive(this);
        UpdateStatus();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Application.AddMessageFilter(this);
        _messageFilterAdded = true;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_messageFilterAdded)
        {
            Application.RemoveMessageFilter(this);
            _messageFilterAdded = false;
        }

        base.OnFormClosed(e);
    }

    public bool PreFilterMessage(ref Message m)
    {
        if (!IsMouseMessage(m.Msg))
        {
            return false;
        }

        Control? source = FromHandle(m.HWnd);
        if (source?.FindForm() != this)
        {
            return false;
        }

        Point screenPoint = GetScreenPointFromMessage(source, m);
        MouseButtons button = GetButtonFromMessage(m.Msg);
        int delta = m.Msg == WmMouseWheel ? GetSignedHighWord(m.WParam) : 0;
        int clicks = IsDoubleClickMessage(m.Msg) ? 2 : 1;

        CollectFromScreen(screenPoint, button, clicks, delta, GetKindFromMessage(m.Msg));

        if (m.Msg == WmLButtonDown && DateTimeOffset.UtcNow - _openedAt > TimeSpan.FromMilliseconds(300))
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        return false;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void BuildUi()
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(48),
            BackColor = BackColor,
            ForeColor = ForeColor
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        Controls.Add(layout);

        _instructionLabel.Text = "マウスを自由に動かしてください。再度左クリックで収集終了します。";
        _instructionLabel.Dock = DockStyle.Fill;
        _instructionLabel.TextAlign = ContentAlignment.MiddleCenter;
        _instructionLabel.Font = new Font("Yu Gothic UI", 22f, FontStyle.Bold);
        _instructionLabel.BackColor = BackColor;
        _instructionLabel.ForeColor = ForeColor;
        layout.Controls.Add(_instructionLabel, 0, 1);

        _countLabel.Dock = DockStyle.Fill;
        _countLabel.TextAlign = ContentAlignment.MiddleCenter;
        _countLabel.Font = new Font("Yu Gothic UI", 13f);
        _countLabel.BackColor = BackColor;
        _countLabel.ForeColor = ForeColor;
        layout.Controls.Add(_countLabel, 0, 2);

        _progress.Dock = DockStyle.Fill;
        _progress.Maximum = _targetEvents;
        layout.Controls.Add(_progress, 0, 3);
    }

    private void WireMouseEventsRecursive(Control control)
    {
        control.MouseMove += FullScreenMouseMove;
        control.MouseDown += FullScreenMouseDown;
        control.MouseUp += FullScreenMouseUp;
        control.MouseDoubleClick += FullScreenMouseDoubleClick;
        control.MouseWheel += FullScreenMouseWheel;

        foreach (Control child in control.Controls)
        {
            WireMouseEventsRecursive(child);
        }
    }

    private void FullScreenMouseMove(object? sender, MouseEventArgs e) => CollectFromControl(sender, e, "FullScreenMouseMove");

    private void FullScreenMouseDown(object? sender, MouseEventArgs e)
    {
        CollectFromControl(sender, e, "FullScreenMouseDown");
        if (e.Button == MouseButtons.Left && DateTimeOffset.UtcNow - _openedAt > TimeSpan.FromMilliseconds(300))
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    private void FullScreenMouseUp(object? sender, MouseEventArgs e) => CollectFromControl(sender, e, "FullScreenMouseUp");

    private void FullScreenMouseDoubleClick(object? sender, MouseEventArgs e) => CollectFromControl(sender, e, "FullScreenMouseDoubleClick");

    private void FullScreenMouseWheel(object? sender, MouseEventArgs e) => CollectFromControl(sender, e, "FullScreenMouseWheel");

    private void CollectFromControl(object? sender, MouseEventArgs e, string kind)
    {
        Control source = sender as Control ?? this;
        Point screenPoint = source.PointToScreen(e.Location);
        CollectFromScreen(screenPoint, e.Button, e.Clicks, e.Delta, kind);
    }

    private void CollectFromScreen(Point screenPoint, MouseButtons button, int clicks, int delta, string kind)
    {
        string category = GetCategory(kind);
        if (IsDuplicateOrStationaryMove(category, screenPoint, button, delta))
        {
            return;
        }

        Point formPoint = PointToClient(screenPoint);
        MouseEventArgs formArgs = new(button, clicks, formPoint.X, formPoint.Y, delta);
        _collector.Collect(formArgs, this, this, kind);
        RememberAcceptedEvent(category, screenPoint, formPoint, button, delta);
        UpdateStatus();
    }

    private bool IsDuplicateOrStationaryMove(string category, Point screenPoint, MouseButtons button, int delta)
    {
        long now = Stopwatch.GetTimestamp();
        long duplicateTicks = Stopwatch.Frequency / 100;

        if (category == "MouseMove" && _lastAcceptedMoveScreenPoint == screenPoint)
        {
            return true;
        }

        if (_lastAcceptedEvent is { } last
            && last.Category == category
            && last.ScreenPoint == screenPoint
            && last.Button == button
            && last.Delta == delta
            && now - last.Timestamp <= duplicateTicks)
        {
            return true;
        }

        return false;
    }

    private void RememberAcceptedEvent(string category, Point screenPoint, Point formPoint, MouseButtons button, int delta)
    {
        _lastAcceptedEvent = new FullScreenMouseSignature(category, screenPoint, formPoint, button, delta, Stopwatch.GetTimestamp());
        if (category == "MouseMove")
        {
            _lastAcceptedMoveScreenPoint = screenPoint;
        }
    }

    private static string GetCategory(string kind)
    {
        if (kind.Contains("MouseMove", StringComparison.Ordinal))
        {
            return "MouseMove";
        }

        if (kind.Contains("MouseDoubleClick", StringComparison.Ordinal))
        {
            return "MouseDoubleClick";
        }

        if (kind.Contains("MouseDown", StringComparison.Ordinal))
        {
            return "MouseDown";
        }

        if (kind.Contains("MouseUp", StringComparison.Ordinal))
        {
            return "MouseUp";
        }

        if (kind.Contains("MouseWheel", StringComparison.Ordinal))
        {
            return "MouseWheel";
        }

        return kind;
    }

    private void UpdateStatus()
    {
        long count = _collector.EventCount;
        _countLabel.Text = $"収集イベント数: {count} / 目標 {_targetEvents}";
        _progress.Value = (int)Math.Min(_targetEvents, count);
    }

    private static bool IsMouseMessage(int message)
    {
        return message is WmMouseMove or WmLButtonDown or WmLButtonUp or WmLButtonDblClk
            or WmRButtonDown or WmRButtonUp or WmRButtonDblClk
            or WmMButtonDown or WmMButtonUp or WmMButtonDblClk or WmMouseWheel;
    }

    private static bool IsDoubleClickMessage(int message)
    {
        return message is WmLButtonDblClk or WmRButtonDblClk or WmMButtonDblClk;
    }

    private static MouseButtons GetButtonFromMessage(int message)
    {
        return message switch
        {
            WmLButtonDown or WmLButtonUp or WmLButtonDblClk => MouseButtons.Left,
            WmRButtonDown or WmRButtonUp or WmRButtonDblClk => MouseButtons.Right,
            WmMButtonDown or WmMButtonUp or WmMButtonDblClk => MouseButtons.Middle,
            _ => MouseButtons.None
        };
    }

    private static string GetKindFromMessage(int message)
    {
        return message switch
        {
            WmMouseMove => "FullScreenFilterMouseMove",
            WmLButtonDown or WmRButtonDown or WmMButtonDown => "FullScreenFilterMouseDown",
            WmLButtonUp or WmRButtonUp or WmMButtonUp => "FullScreenFilterMouseUp",
            WmLButtonDblClk or WmRButtonDblClk or WmMButtonDblClk => "FullScreenFilterMouseDoubleClick",
            WmMouseWheel => "FullScreenFilterMouseWheel",
            _ => "FullScreenFilterMouse"
        };
    }

    private static Point GetScreenPointFromMessage(Control source, Message message)
    {
        if (message.Msg == WmMouseWheel)
        {
            return new Point(GetSignedLowWord(message.LParam), GetSignedHighWord(message.LParam));
        }

        Point clientPoint = new(GetSignedLowWord(message.LParam), GetSignedHighWord(message.LParam));
        return source.PointToScreen(clientPoint);
    }

    private static short GetSignedLowWord(IntPtr value)
    {
        return unchecked((short)((long)value & 0xFFFF));
    }

    private static short GetSignedHighWord(IntPtr value)
    {
        return unchecked((short)(((long)value >> 16) & 0xFFFF));
    }

    private sealed record FullScreenMouseSignature(
        string Category,
        Point ScreenPoint,
        Point FormPoint,
        MouseButtons Button,
        int Delta,
        long Timestamp);
}
