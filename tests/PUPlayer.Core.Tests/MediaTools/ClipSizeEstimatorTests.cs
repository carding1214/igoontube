using PUPlayer.Core.MediaTools;

namespace PUPlayer.Core.Tests.MediaTools;

public sealed class ClipSizeEstimatorTests
{
    [Fact]
    public void Original_EstimatesProportionalBytesWithMargin() =>
        Assert.Equal(115_000_000, ClipSizeEstimator.Estimate(1_000_000_000, 1000, 100, ClipEstimateMode.Original));

    [Fact]
    public void CurrentView_UsesConfiguredBitrateWithMargin() =>
        Assert.Equal(175_260_000, ClipSizeEstimator.Estimate(1, 1, 100, ClipEstimateMode.CurrentView));

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    public void InvalidDuration_IsRejected(double sourceDuration, double selectionDuration) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ClipSizeEstimator.Estimate(100, sourceDuration, selectionDuration, ClipEstimateMode.Original));
}
