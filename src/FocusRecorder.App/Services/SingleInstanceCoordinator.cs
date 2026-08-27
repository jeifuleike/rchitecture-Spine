using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace FocusRecorder.Services;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activationSignal;
    private readonly string _signalName;
    private int _disposed;

    public SingleInstanceCoordinator()
    {
        var scope = GetCurrentSessionScope();
        _signalName = $"Local\\FocusRecorder.Activate.{scope}";
        _mutex = new Mutex(true, $"Local\\FocusRecorder.Instance.{scope}", out var createdNew);
        IsPrimaryInstance = createdNew;
        _activationSignal = new EventWaitHandle(false, EventResetMode.AutoReset, _signalName);
    }

    public bool IsPrimaryInstance { get; }

    public void SignalPrimaryInstance()
    {
        if (!IsPrimaryInstance)
            _activationSignal.Set();
    }

    public void ListenForActivation(Action activate, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activate);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            var handles = new WaitHandle[] { _activationSignal, cancellationToken.WaitHandle };
            while (WaitHandle.WaitAny(handles) == 0)
                activate();
        });
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _activationSignal.Dispose();
        if (IsPrimaryInstance)
            _mutex.ReleaseMutex();
        _mutex.Dispose();
    }

    private static string GetCurrentSessionScope()
    {
        var sessionId = 0;
        try { sessionId = Process.GetCurrentProcess().SessionId; }
        catch (InvalidOperationException) { }

        var source = $"{Environment.UserName}:{sessionId}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))[..16];
        return hash;
    }
}
