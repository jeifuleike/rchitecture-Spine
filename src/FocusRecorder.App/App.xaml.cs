using System.Windows;
using FocusRecorder.Services;

namespace FocusRecorder;

public partial class App : System.Windows.Application
{
    private readonly BackgroundHostService _host = new();
    private readonly CancellationTokenSource _activationCancellation = new();
    private SingleInstanceCoordinator? _singleInstance;
    private TrayController? _tray;
    private MainWindow? _mainWindow;
    private int _isExiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _singleInstance = new SingleInstanceCoordinator();
        if (!_singleInstance.IsPrimaryInstance)
        {
            _singleInstance.SignalPrimaryInstance();
            Shutdown();
            return;
        }

        try
        {
            _mainWindow = new MainWindow(_host);
            _host.Start();
            _tray = new TrayController(ShowMainWindow, ExitApplication, _host);
            _singleInstance.ListenForActivation(() => Dispatcher.BeginInvoke(ShowMainWindow), _activationCancellation.Token);
            _mainWindow.Show();
        }
        catch (Exception)
        {
            _host.MarkUnavailable();
            _mainWindow ??= new MainWindow(_host);
            _mainWindow.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        DisposeShell();
        base.OnExit(e);
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
            _mainWindow = new MainWindow(_host);
        _mainWindow.ShowAndActivate();
    }

    private void ExitApplication()
    {
        if (Interlocked.Exchange(ref _isExiting, 1) != 0)
            return;

        DisposeShell();
        _mainWindow?.AllowClose();
        Shutdown();
    }

    private void DisposeShell()
    {
        _activationCancellation.Cancel();
        _host.Stop();
        _tray?.Dispose();
        _singleInstance?.Dispose();
    }
}
