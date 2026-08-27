using System.ComponentModel;
using System.Windows;
using FocusRecorder.Services;

namespace FocusRecorder;

public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow(BackgroundHostService host)
    {
        InitializeComponent();
        SetStatus(host.Status);
        host.StatusChanged += (_, status) => Dispatcher.Invoke(() => SetStatus(status));
        Closing += OnClosing;
    }

    public void ShowAndActivate()
    {
        Show();
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    public void AllowClose() => _allowClose = true;

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose)
            return;

        try
        {
            e.Cancel = true;
            Hide();
        }
        catch (InvalidOperationException)
        {
            e.Cancel = false;
        }
    }

    private void SetStatus(RecordingStatus status) => StatusText.Text = status.Description;
}
