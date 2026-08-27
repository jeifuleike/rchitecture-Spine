using FocusRecorder.Application;
using FocusRecorder.Domain;
using Xunit;

namespace FocusRecorder.Application.Tests;
public sealed class RecordingCoordinatorTests
{
    [Fact] public async Task Focus_signal_creates_identity_segment_and_checkpoints_every_30_seconds()
    {
        var repo = new FakeRepository(); var coordinator = new RecordingCoordinator(repo); var t = DateTimeOffset.Parse("2026-08-27T00:00:00Z"); var identity = new ApplicationIdentity("Editor", "editor.exe", null);
        await coordinator.HandleAsync(new RecordingSignal(FocusSegmentStates.Focus, identity, t)); await coordinator.HandleAsync(new RecordingSignal(FocusSegmentStates.Focus, identity, t.AddSeconds(29))); Assert.Empty(repo.Checkpoints);
        await coordinator.HandleAsync(new RecordingSignal(FocusSegmentStates.Focus, identity, t.AddSeconds(30)));
        Assert.Single(repo.Saved); Assert.Single(repo.Checkpoints); Assert.Equal(t.AddSeconds(30), repo.Checkpoints[0].time);
    }
    [Fact] public async Task Segment_preserves_creation_local_date_and_offset()
    {
        var repo = new FakeRepository(); var c = new RecordingCoordinator(repo); var local = new DateTimeOffset(2026, 8, 27, 0, 5, 0, TimeSpan.FromHours(8));
        await c.HandleAsync(new RecordingSignal(FocusSegmentStates.Idle, null, local));
        Assert.Equal(new DateOnly(2026, 8, 27), repo.Saved.Single().LocalDate); Assert.Equal(480, repo.Saved.Single().UtcOffsetMinutes); Assert.Equal(local.UtcDateTime, repo.Saved.Single().StartedAtUtc.UtcDateTime);
    }
    [Fact] public async Task Failed_save_does_not_leave_unpersisted_open_segment()
    {
        var repo = new FakeRepository { FailNextSave = true }; var c = new RecordingCoordinator(repo); var t = DateTimeOffset.UtcNow; var identity = new ApplicationIdentity("Editor", "editor.exe", null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.HandleAsync(new RecordingSignal(FocusSegmentStates.Focus, identity, t)));
        await c.HandleAsync(new RecordingSignal(FocusSegmentStates.Focus, identity, t.AddSeconds(31)));
        Assert.Single(repo.Saved); Assert.Empty(repo.Checkpoints);
    }
    [Fact] public async Task Scheduler_checkpoints_without_a_followup_signal_and_stops_cleanly()
    {
        var repo = new FakeRepository(); var c = new RecordingCoordinator(repo, checkpointInterval: TimeSpan.FromMilliseconds(20));
        await c.HandleAsync(new RecordingSignal(FocusSegmentStates.Idle, null, DateTimeOffset.UtcNow));
        await Task.Delay(80); await c.CloseAsync(); var count = repo.Checkpoints.Count; await Task.Delay(50);
        Assert.True(count > 0); Assert.Equal(count, repo.Checkpoints.Count);
    }
    [Fact] public async Task Close_normalizes_a_clock_rollback_to_the_open_boundary()
    {
        var repo = new FakeRepository(); var clock = new MutableClock(); var start = DateTimeOffset.Parse("2026-08-27T00:00:00Z"); var c = new RecordingCoordinator(repo, clock); await c.HandleAsync(new RecordingSignal(FocusSegmentStates.Idle, null, start)); clock.UtcNow = start.AddSeconds(-1);
        await c.CloseAsync(); Assert.Equal(start, repo.Closed.Single().time);
    }
    [Fact] public async Task Same_focus_signal_across_local_date_boundary_splits_the_segment()
    {
        var repo = new FakeRepository(); var c = new RecordingCoordinator(repo); var identity = new ApplicationIdentity("Editor", "editor.exe", null); var first = new DateTimeOffset(2026, 8, 27, 23, 59, 0, TimeSpan.FromHours(8)); var next = first.AddMinutes(2);
        await c.HandleAsync(new RecordingSignal(FocusSegmentStates.Focus, identity, first)); await c.HandleAsync(new RecordingSignal(FocusSegmentStates.Focus, identity, next));
        Assert.Equal(2, repo.Saved.Count); Assert.Single(repo.Closed);
    }
    [Fact] public void Non_positive_checkpoint_interval_is_rejected() => Assert.Throws<ArgumentOutOfRangeException>(() => new RecordingCoordinator(new FakeRepository(), checkpointInterval: TimeSpan.Zero));
    [Theory] [InlineData(FocusSegmentStates.Idle)] [InlineData(FocusSegmentStates.Paused)] [InlineData(FocusSegmentStates.Locked)] [InlineData(FocusSegmentStates.Sleeping)] [InlineData(FocusSegmentStates.Excluded)]
    public async Task Non_focus_signal_has_no_identity(string state) { var repo = new FakeRepository(); var c = new RecordingCoordinator(repo); await c.HandleAsync(new RecordingSignal(state, null, DateTimeOffset.UtcNow)); Assert.Null(repo.Saved.Single().ApplicationIdentity); }
    private sealed class FakeRepository : IRecordingRepository
    {
        public List<FocusSegment> Saved { get; } = []; public List<(Guid id, DateTimeOffset time)> Checkpoints { get; } = []; public List<(Guid id, DateTimeOffset time)> Closed { get; } = []; public bool FailNextSave { get; set; }
        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask; public Task RecoverOpenSegmentsAsync(CancellationToken ct = default) => Task.CompletedTask; public Task<FocusSegment?> GetOpenSegmentAsync(CancellationToken ct = default) => Task.FromResult<FocusSegment?>(null);
        public Task SaveOpenSegmentAsync(FocusSegment s, CancellationToken ct = default) { if (FailNextSave) { FailNextSave = false; throw new InvalidOperationException("write failed"); } Saved.Add(s); return Task.CompletedTask; } public Task CheckpointAsync(Guid id, DateTimeOffset p, CancellationToken ct = default) { Checkpoints.Add((id,p)); return Task.CompletedTask; } public Task CloseAsync(Guid id, DateTimeOffset e, CancellationToken ct = default) { Closed.Add((id, e)); return Task.CompletedTask; }
    }
    private sealed class MutableClock : IClock { public DateTimeOffset UtcNow { get; set; } }
}
