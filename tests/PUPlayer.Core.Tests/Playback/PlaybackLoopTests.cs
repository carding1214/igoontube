using PUPlayer.Core.Playback;

namespace PUPlayer.Core.Tests.Playback;

public sealed class PlaybackLoopTests
{
    [Fact]
    public void ValidRange_RewindsAtEnd()
    {
        var loop = new PlaybackLoop().WithStart(12).WithEnd(18);

        Assert.True(loop.IsActive);
        Assert.Equal(12, loop.SeekTarget(18.1));
        Assert.Null(loop.SeekTarget(17.9));
    }

    [Fact]
    public void InvalidRange_StaysInactive()
    {
        var loop = new PlaybackLoop().WithStart(18).WithEnd(12);

        Assert.False(loop.IsActive);
        Assert.Null(loop.SeekTarget(20));
    }
}
