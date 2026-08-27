using FocusRecorder.Application;
using FocusRecorder.Domain;
using Microsoft.Data.Sqlite;

namespace FocusRecorder.Infrastructure.Sqlite;

public sealed class SqliteRecordingRepository : IRecordingRepository, IDisposable
{
    private readonly string _connectionString;
    private readonly Action? _afterIdentityWrite;
    public SqliteRecordingRepository(string databasePath, Action? afterIdentityWrite = null)
    {
        _connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWriteCreate }.ToString();
        _afterIdentityWrite = afterIdentityWrite;
    }
    public static string DefaultDatabasePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocusRecorder", "focus-recorder.db");
    public void Dispose() { }
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var folder = Path.GetDirectoryName(new SqliteConnectionStringBuilder(_connectionString).DataSource!);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        await using var connection = await OpenConnectionAsync(ct); await using var tx = await connection.BeginTransactionAsync(ct);
        var sql = """
            CREATE TABLE IF NOT EXISTS migrations (id TEXT PRIMARY KEY, applied_at_utc TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS application_identities (id TEXT PRIMARY KEY, stable_key TEXT NOT NULL UNIQUE, display_name TEXT NOT NULL, executable_name TEXT NULL, package_identity TEXT NULL);
            CREATE TABLE IF NOT EXISTS segments (id TEXT PRIMARY KEY, state TEXT NOT NULL CHECK(state IN ('focus','idle','excluded','paused','locked','sleeping')), application_identity_id TEXT NULL, started_at_utc TEXT NOT NULL, ended_at_utc TEXT NULL, checkpoint_at_utc TEXT NULL, local_date TEXT NOT NULL, utc_offset_minutes INTEGER NOT NULL, FOREIGN KEY(application_identity_id) REFERENCES application_identities(id), CHECK((state = 'focus' AND application_identity_id IS NOT NULL) OR (state <> 'focus' AND application_identity_id IS NULL)));
            CREATE TABLE IF NOT EXISTS settings (key TEXT PRIMARY KEY, value TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS structured_logs (id TEXT PRIMARY KEY, occurred_at_utc TEXT NOT NULL, level TEXT NOT NULL CHECK(level IN ('information','warning','error')), event_name TEXT NOT NULL CHECK(event_name IN ('storage_initialized','storage_unavailable','migration_failed','recording_write_failed')), detail TEXT NULL);
            CREATE UNIQUE INDEX IF NOT EXISTS ux_segments_single_open ON segments((1)) WHERE ended_at_utc IS NULL;
            INSERT OR IGNORE INTO migrations(id, applied_at_utc) VALUES ('001-minimal-recording', strftime('%Y-%m-%dT%H:%M:%fZ','now'));
            """;
        await using var command = connection.CreateCommand(); command.Transaction = (SqliteTransaction)tx; command.CommandText = sql; await command.ExecuteNonQueryAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task RecoverOpenSegmentsAsync(CancellationToken ct = default)
    {
        await using var c = await OpenConnectionAsync(ct); await using var tx = await c.BeginTransactionAsync(ct);
        await using var cmd = c.CreateCommand(); cmd.Transaction = (SqliteTransaction)tx;
        cmd.CommandText = "UPDATE segments SET ended_at_utc = COALESCE(checkpoint_at_utc, started_at_utc) WHERE ended_at_utc IS NULL";
        await cmd.ExecuteNonQueryAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<FocusSegment?> GetOpenSegmentAsync(CancellationToken ct = default)
    {
        await using var c = await OpenConnectionAsync(ct); await using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT s.id,s.state,s.started_at_utc,s.ended_at_utc,s.checkpoint_at_utc,s.local_date,s.utc_offset_minutes,a.display_name,a.executable_name,a.package_identity FROM segments s LEFT JOIN application_identities a ON a.id=s.application_identity_id WHERE s.ended_at_utc IS NULL ORDER BY s.started_at_utc DESC, s.id DESC LIMIT 1";
        await using var r = await cmd.ExecuteReaderAsync(ct); if (!await r.ReadAsync(ct)) return null;
        var identity = r.IsDBNull(7) ? null : new ApplicationIdentity(r.GetString(7), r.IsDBNull(8) ? null : r.GetString(8), r.IsDBNull(9) ? null : r.GetString(9));
        return new FocusSegment(Guid.Parse(r.GetString(0)), r.GetString(1), identity, Parse(r.GetString(2)), r.IsDBNull(3) ? null : Parse(r.GetString(3)), r.IsDBNull(4) ? null : Parse(r.GetString(4)), DateOnly.Parse(r.GetString(5)), r.GetInt32(6));
    }
    public async Task SaveOpenSegmentAsync(FocusSegment segment, CancellationToken ct = default)
    {
        segment.Validate();
        if (segment.EndedAtUtc is not null) throw new ArgumentException("只能保存开放片段。", nameof(segment));
        await using var c = await OpenConnectionAsync(ct); await using var tx = await c.BeginTransactionAsync(ct); string? identityId = null;
        if (segment.ApplicationIdentity is { } identity)
        {
            identityId = Guid.NewGuid().ToString(); await using var app = c.CreateCommand(); app.Transaction = (SqliteTransaction)tx;
            app.CommandText = "INSERT INTO application_identities(id,stable_key,display_name,executable_name,package_identity) VALUES($id,$key,$name,$exe,$package) ON CONFLICT(stable_key) DO UPDATE SET display_name=excluded.display_name, executable_name=excluded.executable_name, package_identity=excluded.package_identity; SELECT id FROM application_identities WHERE stable_key=$key;";
            app.Parameters.AddWithValue("$id", identityId); app.Parameters.AddWithValue("$key", identity.StableKey); app.Parameters.AddWithValue("$name", identity.DisplayName); app.Parameters.AddWithValue("$exe", (object?)identity.ExecutableName ?? DBNull.Value); app.Parameters.AddWithValue("$package", (object?)identity.PackageIdentity ?? DBNull.Value);
            identityId = (string)(await app.ExecuteScalarAsync(ct))!;
            _afterIdentityWrite?.Invoke();
        }
        await using var s = c.CreateCommand(); s.Transaction = (SqliteTransaction)tx;
        s.CommandText = "INSERT INTO segments(id,state,application_identity_id,started_at_utc,ended_at_utc,checkpoint_at_utc,local_date,utc_offset_minutes) VALUES($id,$state,$app,$start,NULL,$checkpoint,$date,$offset)";
        s.Parameters.AddWithValue("$id", segment.Id.ToString()); s.Parameters.AddWithValue("$state", segment.State); s.Parameters.AddWithValue("$app", (object?)identityId ?? DBNull.Value); s.Parameters.AddWithValue("$start", Text(segment.StartedAtUtc)); s.Parameters.AddWithValue("$checkpoint", Text(segment.CheckpointAtUtc ?? segment.StartedAtUtc)); s.Parameters.AddWithValue("$date", segment.LocalDate.ToString("O")); s.Parameters.AddWithValue("$offset", segment.UtcOffsetMinutes); await s.ExecuteNonQueryAsync(ct); await tx.CommitAsync(ct);
    }
    public Task CheckpointAsync(Guid id, DateTimeOffset point, CancellationToken ct = default) => ExecuteUpdateAsync("UPDATE segments SET checkpoint_at_utc=$time WHERE id=$id AND ended_at_utc IS NULL AND checkpoint_at_utc <= $time", id, point, ct);
    public Task CloseAsync(Guid id, DateTimeOffset point, CancellationToken ct = default) => ExecuteUpdateAsync("UPDATE segments SET ended_at_utc=$time, checkpoint_at_utc=$time WHERE id=$id AND ended_at_utc IS NULL AND started_at_utc <= $time AND checkpoint_at_utc <= $time", id, point, ct);
    private async Task ExecuteUpdateAsync(string sql, Guid id, DateTimeOffset point, CancellationToken ct)
    {
        if (point.Offset != TimeSpan.Zero) throw new ArgumentException("时间必须为 UTC。", nameof(point));
        await using var c = await OpenConnectionAsync(ct).ConfigureAwait(false); await using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.Parameters.AddWithValue("$id", id.ToString()); cmd.Parameters.AddWithValue("$time", Text(point));
        if (await cmd.ExecuteNonQueryAsync(ct) != 1) throw new InvalidOperationException("目标开放片段不存在或时间边界无效。");
    }
    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(_connectionString); await connection.OpenAsync(ct).ConfigureAwait(false);
        await using var pragma = connection.CreateCommand(); pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;"; await pragma.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        return connection;
    }
    private static DateTimeOffset Parse(string text) => DateTimeOffset.Parse(text, null, System.Globalization.DateTimeStyles.RoundtripKind);
    private static string Text(DateTimeOffset value) => value.ToUniversalTime().ToString("O");
}
