using System.Windows;
using FocusRecorder.Application;
using FocusRecorder.Infrastructure.Sqlite;
using FocusRecorder.Services;

namespace FocusRecorder;

public partial class App : System.Windows.Application
{
    private readonly BackgroundHostService _host = new();
    private readonly CancellationTokenSource _activationCancellation = new();
    private SingleInstanceCoordinator? _singleInstance;
    private TrayController? _tray;
    private MainWindow? _mainWindow;
    private SqliteRecordingRepository? _repository;
    private RecordingCoordinator? _recordingCoordinator;
    private RecordingShellLifecycle? _lifecycle;
    private int _isExiting;

    protected override async void OnStartup(StartupEventArgs e)
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
            _repository = new SqliteRecordingRepository(SqliteRecordingRepository.DefaultDatabasePath);
            _recordingCoordinator = new RecordingCoordinator(_repository);
            await _recordingCoordinator.InitializeAsync();
            _lifecycle = new RecordingShellLifecycle(_host, _recordingCoordinator);
            _mainWindow = new MainWindow(_host, () => _lifecycle.CloseMainWindow(() => _mainWindow?.Hide()));
            _lifecycle.Start();
            _tray = new TrayController(ShowMainWindow, ExitApplication, _host);
            _singleInstance.ListenForActivation(() => Dispatcher.BeginInvoke(ShowMainWindow), _activationCancellation.Token);
            _mainWindow.Show();
        }
        catch (Exception)
        {
            DisposeResources();
            _host.MarkUnavailable();
            _mainWindow ??= new MainWindow(_host, () => _mainWindow?.Hide());
            _tray ??= new TrayController(ShowMainWindow, ExitApplication, _host);
            _mainWindow.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { CloseRecordingShell(); }
        catch { }
        finally { DisposeResources(); base.OnExit(e); }
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

        var completed = false;
        try
        {
            CloseRecordingShell();
            DisposeResources();
            _mainWindow?.AllowClose();
            completed = true;
            Shutdown();
        }
        finally { if (!completed) Interlocked.Exchange(ref _isExiting, 0); }
    }

    private void CloseRecordingShell()
    {
        _activationCancellation.Cancel();
        _lifecycle?.ExitAsync().GetAwaiter().GetResult();
        if (_lifecycle is null)
            _host.Stop();
    }

    private void DisposeResources()
    {
        _activationCancellation.Cancel();
        _tray?.Dispose();
        _singleInstance?.Dispose();
        _repository?.Dispose();
    }
}
