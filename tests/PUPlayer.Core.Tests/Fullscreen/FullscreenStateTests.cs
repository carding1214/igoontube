using PUPlayer.Core.Fullscreen;

namespace PUPlayer.Core.Tests.Fullscreen;

public sealed class FullscreenStateTests
{
    [Fact]
    public void Enter_ShowsControls()
    {
        var state = new FullscreenState();
        state.Enter(DateTimeOffset.UnixEpoch);
        Assert.True(state.IsActive);
        Assert.True(state.AreControlsVisible);
    }

    [Fact]
    public void Tick_HidesControlsAfterTwoSeconds()
    {
        var state = new FullscreenState();
        state.Enter(DateTimeOffset.UnixEpoch);
        state.Tick(DateTimeOffset.UnixEpoch.AddSeconds(2));
        Assert.False(state.AreControlsVisible);
    }

    [Fact]
    public void Move_RestartsHideDelay()
    {
        var state = new FullscreenState();
        state.Enter(DateTimeOffset.UnixEpoch);
        state.Move(DateTimeOffset.UnixEpoch.AddSeconds(1.9));
        state.Tick(DateTimeOffset.UnixEpoch.AddSeconds(3));
        Assert.True(state.AreControlsVisible);
    }

    [Fact]
    public void Exit_ClearsFullscreenAndShowsControls()
    {
        var state = new FullscreenState();
        state.Enter(DateTimeOffset.UnixEpoch);
        state.Exit();
        Assert.False(state.IsActive);
        Assert.True(state.AreControlsVisible);
    }
}
