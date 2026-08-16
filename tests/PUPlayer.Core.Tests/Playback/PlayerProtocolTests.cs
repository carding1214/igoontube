using PUPlayer.Core.Playback;
using PUPlayer.Core.Zoom;
using PUPlayer.Core.MediaTools;

namespace PUPlayer.Core.Tests.Playback;

public sealed class PlayerProtocolTests
{
    [Fact]
    public void Request_RoundTripsWithoutTypeLoss()
    {
        PlayerRequest request = new PlayerRequest.SetTransform(7, new MpvTransform(1, -.25, .1));

        var json = PlayerProtocol.Serialize(request);

        Assert.Equal(request, PlayerProtocol.DeserializeRequest(json));
    }

    [Fact]
    public void AudioFilter_RoundTripsWithoutTypeLoss()
    {
        PlayerRequest request = new PlayerRequest.SetAudioFilter(8, "lavfi=[highpass=f=70]");

        Assert.Equal(request, PlayerProtocol.DeserializeRequest(PlayerProtocol.Serialize(request)));
    }

    [Fact]
    public void Geometry_RoundTripsWithoutTypeLoss()
    {
        PlayerRequest request = new PlayerRequest.SetGeometry(9, new VideoTransform(90, true, false, new(.1, .2, .7, .6)));

        Assert.Equal(request, PlayerProtocol.DeserializeRequest(PlayerProtocol.Serialize(request)));
    }

    [Fact]
    public void CapturedFrame_RoundTripsWithRequestIdAndPixels()
    {
        PlayerEvent value = new PlayerEvent.FrameCaptured(12, new VideoFrame(2, 1, [1, 2, 3, 4, 5, 6]));

        var result = Assert.IsType<PlayerEvent.FrameCaptured>(PlayerProtocol.DeserializeEvent(PlayerProtocol.Serialize(value)));
        Assert.Equal(12, result.RequestId);
        Assert.Equal([1, 2, 3, 4, 5, 6], result.Frame.Rgb24);
    }

    [Theory]
    [InlineData("https://example.com/video.mp4")]
    [InlineData("file:///F:/video.mp4")]
    public void Load_RejectsUrls(string value) =>
        Assert.Throws<ArgumentException>(() => PlayerRequest.Load.Create(1, value));
}
