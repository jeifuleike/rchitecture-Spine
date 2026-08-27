namespace FocusRecorder.Services;

public enum RecordingState
{
    NotStarted,
    Running,
    Stopped,
    Unavailable
}

public sealed record RecordingStatus(RecordingState State, string Description)
{
    public static RecordingStatus NotStarted { get; } = new(RecordingState.NotStarted, "尚未开始");
    public static RecordingStatus Running { get; } = new(RecordingState.Running, "后台宿主正在运行");
    public static RecordingStatus Stopped { get; } = new(RecordingState.Stopped, "后台宿主已停止");
    public static RecordingStatus Unavailable { get; } = new(RecordingState.Unavailable, "记录状态暂时不可用");
}
