using FocusRecorder.Domain;

namespace FocusRecorder.Application;

public interface IRecordingRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task RecoverOpenSegmentsAsync(CancellationToken cancellationToken = default);
    Task<FocusSegment?> GetOpenSegmentAsync(CancellationToken cancellationToken = default);
    Task SaveOpenSegmentAsync(FocusSegment segment, CancellationToken cancellationToken = default);
    Task CheckpointAsync(Guid segmentId, DateTimeOffset checkpointUtc, CancellationToken cancellationToken = default);
    Task CloseAsync(Guid segmentId, DateTimeOffset endedAtUtc, CancellationToken cancellationToken = default);
}

public interface IClock { DateTimeOffset UtcNow { get; } }
public sealed class SystemClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
public sealed record RecordingSignal(string State, ApplicationIdentity? ApplicationIdentity, DateTimeOffset OccurredAtUtc);

public sealed class RecordingCoordinator
{
    private static readonly TimeSpan DefaultCheckpointInterval = TimeSpan.FromSeconds(30);
    private readonly IRecordingRepository _repository;
    private readonly IClock _clock;
    private readonly SemaphoreSlim _sequence = new(1, 1);
    private readonly TimeSpan _checkpointInterval;
    private FocusSegment? _open;
    private CancellationTokenSource? _checkpointCancellation;
    private Task? _checkpointTask;
    private int _initialized;
    private int _shutdownState;

    public RecordingCoordinator(IRecordingRepository repository, IClock? clock = null, TimeSpan? checkpointInterval = null)
    {
        _repository = repository;
        _clock = clock ?? new SystemClock();
        _checkpointInterval = checkpointInterval ?? DefaultCheckpointInterval;
        if (_checkpointInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(checkpointInterval));
    }
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0) return;
        try { await _sequence.WaitAsync(ct).ConfigureAwait(false); try { await _repository.InitializeAsync(ct).ConfigureAwait(false); await _repository.RecoverOpenSegmentsAsync(ct).ConfigureAwait(false); _open = null; } finally { _sequence.Release(); } }
        catch { Interlocked.Exchange(ref _initialized, 0); throw; }
    }
    public async Task HandleAsync(RecordingSignal signal, CancellationToken ct = default)
    {
        var localDate = DateOnly.FromDateTime(signal.OccurredAtUtc.DateTime);
        var utcOffsetMinutes = (int)signal.OccurredAtUtc.Offset.TotalMinutes;
        signal = signal with { OccurredAtUtc = signal.OccurredAtUtc.ToUniversalTime() };
        var probe = new FocusSegment(Guid.NewGuid(), signal.State, signal.ApplicationIdentity, signal.OccurredAtUtc, null, signal.OccurredAtUtc, localDate, utcOffsetMinutes); probe.Validate();
        await _sequence.WaitAsync(ct);
        try
        {
            if (Volatile.Read(ref _shutdownState) != 0) throw new InvalidOperationException("记录协调器已关闭。");
            if (_open is not null && signal.OccurredAtUtc < (_open.CheckpointAtUtc ?? _open.StartedAtUtc)) return;
            if (_open is not null && _open.State == signal.State && Equals(_open.ApplicationIdentity?.StableKey, signal.ApplicationIdentity?.StableKey) && _open.LocalDate == localDate && _open.UtcOffsetMinutes == utcOffsetMinutes)
            {
                if (signal.OccurredAtUtc - (_open.CheckpointAtUtc ?? _open.StartedAtUtc) >= _checkpointInterval) { await _repository.CheckpointAsync(_open.Id, signal.OccurredAtUtc, ct).ConfigureAwait(false); _open = _open with { CheckpointAtUtc = signal.OccurredAtUtc }; }
                return;
            }
            if (_open is not null)
            {
                await _repository.CloseAsync(_open.Id, signal.OccurredAtUtc, ct).ConfigureAwait(false);
                _open = null;
                _ = StopCheckpointTimer();
            }
            await _repository.SaveOpenSegmentAsync(probe, ct).ConfigureAwait(false);
            _open = probe;
            StartCheckpointTimer();
        }
        finally { _sequence.Release(); }
    }
    public async Task CloseAsync(CancellationToken ct = default)
    {
        if (Interlocked.CompareExchange(ref _shutdownState, 1, 0) != 0) return;
        var task = StopCheckpointTimer();
        try
        {
            if (task is not null) { try { await task.ConfigureAwait(false); } catch (OperationCanceledException) { } }
            await _sequence.WaitAsync(ct).ConfigureAwait(false);
            try { if (_open is not null) { var end = _clock.UtcNow.ToUniversalTime(); if (end < (_open.CheckpointAtUtc ?? _open.StartedAtUtc)) end = _open.CheckpointAtUtc ?? _open.StartedAtUtc; await _repository.CloseAsync(_open.Id, end, ct).ConfigureAwait(false); _open = null; } }
            finally { _sequence.Release(); }
        }
        catch
        {
            if (_open is not null) StartCheckpointTimer();
            Interlocked.Exchange(ref _shutdownState, 0);
            throw;
        }
    }
    private void StartCheckpointTimer()
    {
        StopCheckpointTimer(); _checkpointCancellation = new CancellationTokenSource(); var token = _checkpointCancellation.Token;
        _checkpointTask = Task.Run(async () => { try { while (true) { await Task.Delay(_checkpointInterval, token).ConfigureAwait(false); await _sequence.WaitAsync(token).ConfigureAwait(false); try { if (_open is null || Volatile.Read(ref _shutdownState) != 0) return; var point = _clock.UtcNow.ToUniversalTime(); var floor = _open.CheckpointAtUtc ?? _open.StartedAtUtc; if (point < floor) continue; try { await _repository.CheckpointAsync(_open.Id, point, token).ConfigureAwait(false); _open = _open with { CheckpointAtUtc = point }; } catch (Exception) when (!token.IsCancellationRequested) { } } finally { _sequence.Release(); } } } catch (OperationCanceledException) { } }, CancellationToken.None);
    }
    private Task? StopCheckpointTimer() { var cancellation = Interlocked.Exchange(ref _checkpointCancellation, null); cancellation?.Cancel(); cancellation?.Dispose(); var task = Interlocked.Exchange(ref _checkpointTask, null); return task; }
}
