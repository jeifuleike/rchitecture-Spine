using FocusRecorder.Domain;
using Xunit;

namespace FocusRecorder.Domain.Tests;
public sealed class FocusSegmentTests
{
    [Fact] public void Focus_requires_identity() => Assert.Throws<ArgumentException>(() => new FocusSegment(Guid.NewGuid(), FocusSegmentStates.Focus, null, DateTimeOffset.UtcNow, null, null, DateOnly.FromDateTime(DateTime.UtcNow), 0).Validate());
    [Fact] public void Non_focus_rejects_identity() => Assert.Throws<ArgumentException>(() => new FocusSegment(Guid.NewGuid(), FocusSegmentStates.Idle, new ApplicationIdentity("Editor", "editor.exe", null), DateTimeOffset.UtcNow, null, null, DateOnly.FromDateTime(DateTime.UtcNow), 0).Validate());
    [Fact] public void Closed_segment_rejects_checkpoint_after_end() { var t = DateTimeOffset.UtcNow; Assert.Throws<ArgumentException>(() => new FocusSegment(Guid.NewGuid(), FocusSegmentStates.Idle, null, t, t.AddSeconds(1), t.AddSeconds(2), DateOnly.FromDateTime(t.UtcDateTime), 0).Validate()); }
    [Theory] [InlineData("C:\\secret", "app.exe", null)] [InlineData("Editor", "C:\\app.exe", null)] [InlineData("Editor", "app\n.exe", null)] [InlineData("Editor", null, "package/name")]
    public void Identity_rejects_path_like_fields(string display, string? executable, string? package) => Assert.Throws<ArgumentException>(() => new ApplicationIdentity(display, executable, package));
    [Fact] public void Executable_identity_key_does_not_change_when_display_name_changes()
    {
        Assert.Equal(new ApplicationIdentity("Editor", "editor.exe", null).StableKey, new ApplicationIdentity("Editor Preview", "editor.exe", null).StableKey);
    }
}
