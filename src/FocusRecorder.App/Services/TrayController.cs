using System.Drawing;
using System.Windows.Forms;

namespace FocusRecorder.Services;

public sealed class TrayController : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusItem;
    private int _disposed;

    public TrayController(Action openReport, Action exit, BackgroundHostService host)
    {
        ArgumentNullException.ThrowIfNull(openReport);
        ArgumentNullException.ThrowIfNull(exit);
        ArgumentNullException.ThrowIfNull(host);

        _statusItem = new ToolStripMenuItem();
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("打开报表", null, (_, _) => openReport()));
        menu.Items.Add(_statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("退出", null, (_, _) => exit()));

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "专注记录器",
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => openReport();
        host.StatusChanged += OnStatusChanged;
        SetStatus(host.Status);
    }

    public bool IsAvailable => Volatile.Read(ref _disposed) == 0 && _notifyIcon.Visible;

    public void SetStatus(RecordingStatus status)
    {
        _statusItem.Text = $"状态：{status.Description}";
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private void OnStatusChanged(object? sender, RecordingStatus status) => SetStatus(status);
}
