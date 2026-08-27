using FocusRecorder.Services;
using Xunit;

namespace FocusRecorder.App.Tests;

public sealed class BackgroundHostServiceTests
{
    [Fact]
    public void Start_moves_host_to_running()
    {
        var host = new BackgroundHostService();

        host.Start();

        Assert.Equal(RecordingState.Running, host.Status.State);
    }

    [Fact]
    public void Stop_moves_running_host_to_stopped_once()
    {
        var host = new BackgroundHostService();
        host.Start();

        host.Stop();
        host.Stop();

        Assert.Equal(RecordingState.Stopped, host.Status.State);
        Assert.Equal(1, host.StopCount);
    }

    [Fact]
    public void Stopped_host_does_not_restart()
    {
        var host = new BackgroundHostService();
        host.Start();
        host.Stop();

        host.Start();

        Assert.Equal(RecordingState.Stopped, host.Status.State);
        Assert.Equal(1, host.StopCount);
    }

    [Fact]
    public void Concurrent_lifecycle_calls_leave_a_valid_terminal_state()
    {
        var host = new BackgroundHostService();

        Parallel.Invoke(host.Start, host.Start, host.Stop, host.Stop);

        Assert.Contains(host.Status.State, new[] { RecordingState.Running, RecordingState.Stopped });
        Assert.InRange(host.StopCount, 0, 1);
    }

    [Fact]
    public void Status_changes_are_reported()
    {
        var host = new BackgroundHostService();
        var observed = new List<RecordingState>();
        host.StatusChanged += (_, status) => observed.Add(status.State);

        host.Start();
        host.Stop();

        Assert.Equal(new[] { RecordingState.Running, RecordingState.Stopped }, observed);
    }
}
