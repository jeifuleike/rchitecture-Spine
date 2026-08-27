using FocusRecorder.Domain;
using FocusRecorder.Application;
using FocusRecorder.Infrastructure.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace FocusRecorder.Infrastructure.Tests;
public sealed class SqliteRecordingRepositoryTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"focus-{Guid.NewGuid():N}.db");
    [Fact] public async Task Initialize_creates_only_minimal_schema()
    {
        var repo = new SqliteRecordingRepository(_path); await repo.InitializeAsync();
        await using var c = new SqliteConnection($"Data Source={_path}"); await c.OpenAsync(); await using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name"; await using var r = await cmd.ExecuteReaderAsync(); var names = new List<string>(); while(await r.ReadAsync()) names.Add(r.GetString(0));
        Assert.Equal(["application_identities", "migrations", "segments", "settings", "structured_logs"], names);
    }
    [Fact] public async Task Recovery_closes_open_segment_at_checkpoint()
    {
        var repo = new SqliteRecordingRepository(_path); await repo.InitializeAsync(); var t = DateTimeOffset.Parse("2026-08-27T00:00:00Z"); var segment = new FocusSegment(Guid.NewGuid(), FocusSegmentStates.Focus, new ApplicationIdentity("Editor", "editor.exe", null), t, null, t.AddSeconds(30), DateOnly.FromDateTime(t.UtcDateTime), 0); await repo.SaveOpenSegmentAsync(segment); await repo.RecoverOpenSegmentsAsync();
        await using var c = new SqliteConnection($"Data Source={_path}"); await c.OpenAsync(); await using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT ended_at_utc FROM segments WHERE id=$id"; cmd.Parameters.AddWithValue("$id", segment.Id.ToString()); Assert.Equal(t.AddSeconds(30).ToString("O"), (string?)await cmd.ExecuteScalarAsync());
    }
    [Fact] public async Task Failed_segment_write_rolls_back_identity_and_segment()
    {
        var repo = new SqliteRecordingRepository(_path, () => throw new InvalidOperationException("simulated write failure"));
        await repo.InitializeAsync(); var now = DateTimeOffset.UtcNow;
        var segment = new FocusSegment(Guid.NewGuid(), FocusSegmentStates.Focus, new ApplicationIdentity("Editor", "editor.exe", null), now, null, now, DateOnly.FromDateTime(now.UtcDateTime), 0);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SaveOpenSegmentAsync(segment));
        await using var c = new SqliteConnection($"Data Source={_path}"); await c.OpenAsync();
        foreach (var table in new[] { "application_identities", "segments" }) { await using var cmd = c.CreateCommand(); cmd.CommandText = $"SELECT COUNT(*) FROM {table}"; Assert.Equal(0L, Convert.ToInt64(await cmd.ExecuteScalarAsync())); }
    }
    [Fact] public async Task Coordinator_initialization_recovers_existing_open_segment()
    {
        var repo = new SqliteRecordingRepository(_path); await repo.InitializeAsync(); var now = DateTimeOffset.UtcNow; var segment = new FocusSegment(Guid.NewGuid(), FocusSegmentStates.Idle, null, now, null, now.AddSeconds(1), DateOnly.FromDateTime(now.UtcDateTime), 0); await repo.SaveOpenSegmentAsync(segment);
        await new RecordingCoordinator(repo).InitializeAsync();
        Assert.Null(await repo.GetOpenSegmentAsync());
    }
    [Fact] public async Task Sqlite_checkpoint_and_close_persist_utc_boundaries()
    {
        var repo = new SqliteRecordingRepository(_path); await repo.InitializeAsync(); var start = DateTimeOffset.Parse("2026-08-27T00:00:00Z"); var segment = new FocusSegment(Guid.NewGuid(), FocusSegmentStates.Idle, null, start, null, start, DateOnly.FromDateTime(start.UtcDateTime), 0); await repo.SaveOpenSegmentAsync(segment); await repo.CheckpointAsync(segment.Id, start.AddSeconds(30)); await repo.CloseAsync(segment.Id, start.AddSeconds(45));
        await using var c = new SqliteConnection($"Data Source={_path}"); await c.OpenAsync(); await using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT checkpoint_at_utc,ended_at_utc FROM segments WHERE id=$id"; cmd.Parameters.AddWithValue("$id", segment.Id.ToString()); await using var reader = await cmd.ExecuteReaderAsync(); Assert.True(await reader.ReadAsync()); Assert.Equal(start.AddSeconds(45).ToString("O"), reader.GetString(0)); Assert.Equal(start.AddSeconds(45).ToString("O"), reader.GetString(1));
    }
    [Fact] public async Task Mutation_rejects_missing_segment_and_non_utc_time()
    {
        var repo = new SqliteRecordingRepository(_path); await repo.InitializeAsync(); await Assert.ThrowsAsync<InvalidOperationException>(() => repo.CheckpointAsync(Guid.NewGuid(), DateTimeOffset.UtcNow)); await Assert.ThrowsAsync<ArgumentException>(() => repo.CloseAsync(Guid.NewGuid(), new DateTimeOffset(2026, 8, 27, 1, 0, 0, TimeSpan.FromHours(8))));
    }
    [Fact] public async Task Close_rejects_time_before_segment_start()
    {
        var repo = new SqliteRecordingRepository(_path); await repo.InitializeAsync(); var start = DateTimeOffset.UtcNow; var segment = new FocusSegment(Guid.NewGuid(), FocusSegmentStates.Idle, null, start, null, start, DateOnly.FromDateTime(start.UtcDateTime), 0); await repo.SaveOpenSegmentAsync(segment);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.CloseAsync(segment.Id, start.AddSeconds(-1)));
    }
    [Fact] public async Task Filename_only_database_path_does_not_require_a_directory()
    {
        var filename = $"focus-filename-{Guid.NewGuid():N}.db";
        try { var repo = new SqliteRecordingRepository(filename); await repo.InitializeAsync(); Assert.True(File.Exists(filename)); }
        finally { SqliteConnection.ClearAllPools(); if (File.Exists(filename)) File.Delete(filename); }
    }
    [Fact] public async Task Close_rejects_time_before_the_last_checkpoint()
    {
        var repo = new SqliteRecordingRepository(_path); await repo.InitializeAsync(); var start = DateTimeOffset.UtcNow; var segment = new FocusSegment(Guid.NewGuid(), FocusSegmentStates.Idle, null, start, null, start, DateOnly.FromDateTime(start.UtcDateTime), 0); await repo.SaveOpenSegmentAsync(segment); await repo.CheckpointAsync(segment.Id, start.AddSeconds(30));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.CloseAsync(segment.Id, start.AddSeconds(15)));
    }
    [Fact] public async Task Saving_a_closed_segment_is_rejected()
    {
        var repo = new SqliteRecordingRepository(_path); await repo.InitializeAsync(); var start = DateTimeOffset.UtcNow; var segment = new FocusSegment(Guid.NewGuid(), FocusSegmentStates.Idle, null, start, start.AddSeconds(1), start, DateOnly.FromDateTime(start.UtcDateTime), 0);
        await Assert.ThrowsAsync<ArgumentException>(() => repo.SaveOpenSegmentAsync(segment));
    }
    public void Dispose() { SqliteConnection.ClearAllPools(); if (File.Exists(_path)) File.Delete(_path); }
}
