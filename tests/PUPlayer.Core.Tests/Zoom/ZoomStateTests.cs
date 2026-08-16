using PUPlayer.Core.Zoom;

namespace PUPlayer.Core.Tests.Zoom;

public sealed class ZoomStateTests
{
    [Fact]
    public void ZoomAt_KeepsCursorSourcePointStable()
    {
        var cursor = new NormalizedPoint(.75, .5);

        var next = ZoomState.Default.ZoomAt(2, cursor);

        Assert.Equal(2, next.Scale);
        Assert.Equal(.625, next.CenterX, 3);
    }

    [Fact]
    public void ZoomAndPan_AreClamped()
    {
        var state = ZoomState.Default.ZoomAt(99, new(1, 1)).PanBy(5, 5);

        Assert.Equal(8, state.Scale);
        Assert.InRange(state.CenterX, .0625, .9375);
        Assert.InRange(state.CenterY, .0625, .9375);
    }
}
