namespace FocusRecorder.Services;

/// <summary>为后续记录协调器提供无外部副作用的生命周期边界。</summary>
public sealed class BackgroundHostService
{
    private readonly object _gate = new();
    private RecordingStatus _status = RecordingStatus.NotStarted;
    private int _stopCount;

    public event EventHandler<RecordingStatus>? StatusChanged;

    public RecordingStatus Status
    {
        get { lock (_gate) return _status; }
    }

    public int StopCount => Volatile.Read(ref _stopCount);

    public void Start()
    {
        RecordingStatus? changedStatus = null;
        lock (_gate)
        {
            if (_status.State != RecordingState.NotStarted)
                return;

            _status = RecordingStatus.Running;
            changedStatus = _status;
        }

        StatusChanged?.Invoke(this, changedStatus);
    }

    public void Stop()
    {
        RecordingStatus? changedStatus = null;
        lock (_gate)
        {
            if (_status.State != RecordingState.Running)
                return;

            _status = RecordingStatus.Stopped;
            Interlocked.Increment(ref _stopCount);
            changedStatus = _status;
        }

        StatusChanged?.Invoke(this, changedStatus);
    }

    public void MarkUnavailable()
    {
        SetStatus(RecordingStatus.Unavailable);
    }

    private void SetStatus(RecordingStatus next)
    {
        lock (_gate)
        {
            if (_status == next)
                return;
            _status = next;
        }

        StatusChanged?.Invoke(this, next);
    }
}
