using PUPlayer.App.Playback;
using PUPlayer.Core.Zoom;

namespace PUPlayer.IntegrationTests.Zoom;

public sealed class ZoomInteractionTests
{
    [Fact]
    public void Wheel_UsesCursorAndNeverExceedsEightTimes()
    {
        var interaction = new ZoomInteraction();

        for (var i = 0; i < 30; i++) interaction.Wheel(120, new(.8, .3));

        Assert.Equal(8, interaction.State.Scale);
    }

    [Fact]
    public void DoubleClick_ResetsTransform()
    {
        var interaction = new ZoomInteraction();
        interaction.Wheel(120, new(.7, .5));

        interaction.Reset();

        Assert.Equal(ZoomState.Default, interaction.State);
    }
}
