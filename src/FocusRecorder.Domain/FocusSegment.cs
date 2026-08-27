namespace FocusRecorder.Domain;

public static class FocusSegmentStates
{
    public const string Focus = "focus";
    public const string Idle = "idle";
    public const string Excluded = "excluded";
    public const string Paused = "paused";
    public const string Locked = "locked";
    public const string Sleeping = "sleeping";
    public static bool IsValid(string state) => state is Focus or Idle or Excluded or Paused or Locked or Sleeping;
}

public sealed record ApplicationIdentity
{
    public ApplicationIdentity(string displayName, string? executableName, string? packageIdentity)
    {
        if (string.IsNullOrWhiteSpace(displayName) || ContainsUnsafeText(displayName)) throw new ArgumentException("应用显示名无效。");
        if (!string.IsNullOrWhiteSpace(executableName) && (ContainsPathSeparator(executableName) || executableName.Any(char.IsControl))) throw new ArgumentException("可执行文件名无效。");
        if (!string.IsNullOrWhiteSpace(packageIdentity) && (ContainsPathSeparator(packageIdentity) || packageIdentity.Any(char.IsControl))) throw new ArgumentException("包标识无效。");
        if (string.IsNullOrWhiteSpace(executableName) && string.IsNullOrWhiteSpace(packageIdentity)) throw new ArgumentException("应用身份必须包含可执行文件名或包标识。");
        DisplayName = displayName.Trim(); ExecutableName = executableName?.Trim(); PackageIdentity = packageIdentity?.Trim();
    }
    public string DisplayName { get; }
    public string? ExecutableName { get; }
    public string? PackageIdentity { get; }
    public string StableKey
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DisplayName) || (string.IsNullOrWhiteSpace(ExecutableName) && string.IsNullOrWhiteSpace(PackageIdentity)))
                throw new ArgumentException("应用身份必须包含显示名及可执行文件名或包标识。");
            return !string.IsNullOrWhiteSpace(PackageIdentity)
                ? $"package:{PackageIdentity.Trim().ToLowerInvariant()}"
                : $"exe:{ExecutableName!.Trim().ToLowerInvariant()}";
        }
    }
    private static bool ContainsUnsafeText(string value) => value.Any(char.IsControl) || ContainsPathSeparator(value);
    private static bool ContainsPathSeparator(string value) => value.Contains('/') || value.Contains('\\');
}

public sealed record FocusSegment(Guid Id, string State, ApplicationIdentity? ApplicationIdentity, DateTimeOffset StartedAtUtc, DateTimeOffset? EndedAtUtc, DateTimeOffset? CheckpointAtUtc, DateOnly LocalDate, int UtcOffsetMinutes)
{
    public void Validate()
    {
        if (Id == Guid.Empty) throw new ArgumentException("片段标识无效。");
        if (!FocusSegmentStates.IsValid(State)) throw new ArgumentException("片段状态无效。");
        if (StartedAtUtc.Offset != TimeSpan.Zero || (EndedAtUtc.HasValue && EndedAtUtc.Value.Offset != TimeSpan.Zero) || (CheckpointAtUtc.HasValue && CheckpointAtUtc.Value.Offset != TimeSpan.Zero)) throw new ArgumentException("片段边界必须为 UTC。");
        if (UtcOffsetMinutes is < -840 or > 840 || DateOnly.FromDateTime(StartedAtUtc.UtcDateTime.AddMinutes(UtcOffsetMinutes)) != LocalDate) throw new ArgumentException("本地日期或 UTC 偏移无效。");
        if (EndedAtUtc < StartedAtUtc || CheckpointAtUtc < StartedAtUtc) throw new ArgumentException("片段边界无效。");
        if (EndedAtUtc.HasValue && CheckpointAtUtc.HasValue && CheckpointAtUtc > EndedAtUtc) throw new ArgumentException("检查点不得晚于结束时间。");
        if ((State == FocusSegmentStates.Focus) != (ApplicationIdentity is not null)) throw new ArgumentException("focus 必须且只能携带应用身份。");
    }
}
