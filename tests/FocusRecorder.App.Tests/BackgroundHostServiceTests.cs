using FocusRecorder.Services;
using FocusRecorder.Application;
using FocusRecorder.Domain;
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

    [Fact]
    public async Task Main_window_close_hides_window_and_keeps_recording()
    {
        var host = new BackgroundHostService();
        var repository = new FakeRepository();
        var lifecycle = new RecordingShellLifecycle(host, new RecordingCoordinator(repository));
        lifecycle.Start();
        var hidden = false;

        lifecycle.CloseMainWindow(() => hidden = true);

        Assert.True(hidden);
        Assert.Equal(RecordingState.Running, host.Status.State);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Tray_exit_closes_open_segment_before_stopping_host()
    {
        var host = new BackgroundHostService();
        var repository = new FakeRepository();
        var coordinator = new RecordingCoordinator(repository);
        var lifecycle = new RecordingShellLifecycle(host, coordinator);
        lifecycle.Start();
        await coordinator.HandleAsync(new RecordingSignal(FocusSegmentStates.Focus, new FocusRecorder.Domain.ApplicationIdentity("Editor", "editor.exe", null), DateTimeOffset.UtcNow));

        await lifecycle.ExitAsync();

        Assert.Single(repository.Closed);
        Assert.Equal(RecordingState.Stopped, host.Status.State);
    }
    [Fact] public async Task Failed_exit_can_be_retried()
    {
        var host = new BackgroundHostService(); var repository = new FakeRepository { FailClose = true }; var coordinator = new RecordingCoordinator(repository); var lifecycle = new RecordingShellLifecycle(host, coordinator); lifecycle.Start();
        await coordinator.HandleAsync(new RecordingSignal(FocusSegmentStates.Idle, null, DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<InvalidOperationException>(() => lifecycle.ExitAsync()); Assert.Equal(RecordingState.Running, host.Status.State);
        repository.FailClose = false; await lifecycle.ExitAsync(); Assert.Equal(RecordingState.Stopped, host.Status.State);
    }

    private sealed class FakeRepository : IRecordingRepository
    {
        public List<Guid> Closed { get; } = []; public bool FailClose { get; set; }
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RecoverOpenSegmentsAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<FocusSegment?> GetOpenSegmentAsync(CancellationToken ct = default) => Task.FromResult<FocusSegment?>(null);
        public Task SaveOpenSegmentAsync(FocusSegment segment, CancellationToken ct = default) => Task.CompletedTask;
        public Task CheckpointAsync(Guid segmentId, DateTimeOffset checkpointUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task CloseAsync(Guid segmentId, DateTimeOffset endedAtUtc, CancellationToken ct = default) { if (FailClose) throw new InvalidOperationException("close failed"); Closed.Add(segmentId); return Task.CompletedTask; }
    }
}
