using PUPlayer.Core.MediaTools;

namespace PUPlayer.Core.Tests.MediaTools;

public sealed class MediaToolModelsTests
{
    [Fact]
    public void Selection_NormalizesMarks() =>
        Assert.Equal(new ClipSelection(10, 20), ClipSelection.FromMarks(20, 10, 100));

    [Fact]
    public void Selection_RejectsTinyClips() =>
        Assert.Throws<ArgumentException>(() => ClipSelection.FromMarks(10, 10.1, 100));

    [Theory]
    [InlineData(120, 60)]
    [InlineData(7200, 180)]
    [InlineData(50000, 300)]
    public void ThumbnailCount_Adapts(double duration, int expected) =>
        Assert.Equal(expected, ThumbnailPlan.ForDuration(duration).Count);

    [Fact]
    public void OutputName_IncrementsWithoutOverwrite()
    {
        var occupied = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"F:\media\sample_clip_001.mp4"
        };

        var result = ClipOutputNamer.Next(@"F:\media\sample.mkv", occupied.Contains);

        Assert.Equal(@"F:\media\sample_clip_002.mp4", result);
    }

    [Fact]
    public void Transform_FilterContainsSelectedOperations()
    {
        var value = new VideoTransform(90, true, false, new(.1, .2, .7, .6));

        Assert.Equal("crop=iw*0.7:ih*0.6:iw*0.1:ih*0.2,hflip,transpose=1", value.ToFfmpegFilter());
    }

    [Fact]
    public void Transform_RejectsInvalidCrop() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new VideoTransform(0, false, false, new(.9, .2, .2, .6)));
}
