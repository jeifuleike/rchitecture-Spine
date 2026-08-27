using FocusRecorder.Application;

namespace FocusRecorder.Services;

/// <summary>将壳的关闭语义与记录协调器隔离，便于在不启动 Win32/WPF 的情况下验证。</summary>
public sealed class RecordingShellLifecycle
{
    private readonly BackgroundHostService _host;
    private readonly RecordingCoordinator _coordinator;
    private int _exited;
    private int _exiting;

    public RecordingShellLifecycle(BackgroundHostService host, RecordingCoordinator coordinator)
    {
        _host = host;
        _coordinator = coordinator;
    }

    public void Start() => _host.Start();

    public void CloseMainWindow(Action hideWindow)
    {
        ArgumentNullException.ThrowIfNull(hideWindow);
        hideWindow();
    }

    public async Task ExitAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _exited) != 0 || Interlocked.CompareExchange(ref _exiting, 1, 0) != 0)
            return;

        try
        {
            await _coordinator.CloseAsync(cancellationToken);
            _host.Stop();
            Interlocked.Exchange(ref _exited, 1);
        }
        finally
        {
            if (Volatile.Read(ref _exited) == 0) Interlocked.Exchange(ref _exiting, 0);
        }
    }
}
