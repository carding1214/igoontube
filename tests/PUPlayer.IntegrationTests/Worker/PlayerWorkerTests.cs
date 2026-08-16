using System.IO;
using PUPlayer.Core.Playback;
using PUPlayer.Core.Zoom;
using PUPlayer.MpvWorker.Interop;
using PUPlayer.MpvWorker.Worker;
using PUPlayer.Core.MediaTools;

namespace PUPlayer.IntegrationTests.Worker;

public sealed class PlayerWorkerTests
{
    [Fact]
    public void PlaybackOptions_AreBalancedAndBounded()
    {
        var expected = new Dictionary<string, string>
        {
            ["vo"] = "gpu-next", ["hwdec"] = "auto-safe", ["volume-max"] = "200", ["config"] = "no",
            ["osc"] = "no", ["input-default-bindings"] = "no", ["input-vo-keyboard"] = "no", ["terminal"] = "no",
            ["save-position-on-quit"] = "no", ["idle"] = "yes", ["demuxer-thread"] = "yes", ["cache"] = "yes",
            ["cache-secs"] = "5", ["demuxer-readahead-secs"] = "5", ["demuxer-max-bytes"] = "64MiB",
            ["demuxer-max-back-bytes"] = "16MiB", ["cache-pause-initial"] = "no"
        };

        Assert.Equal(expected, MpvPlaybackOptions.Values);
    }

    [Fact]
    public async Task Commands_AreAppliedOnlyToTheOwnedClient()
    {
        var mpv = new RecordingMpvClient();
        var worker = new PlayerWorker(mpv);

        await worker.ApplyAsync(new PlayerRequest.SetVolume(1, 175), default);
        await worker.ApplyAsync(new PlayerRequest.Seek(2, 42.5), default);

        Assert.Equal(["volume:175", "seek:42.5"], mpv.Calls);
    }

    [Fact]
    public async Task Shutdown_DisposesMpv()
    {
        var mpv = new RecordingMpvClient();
        var worker = new PlayerWorker(mpv);

        await worker.ApplyAsync(new PlayerRequest.Shutdown(1), default);

        Assert.True(mpv.Disposed);
    }

    [Fact]
    public async Task AudioCommands_AreAppliedToOwnedClient()
    {
        var root = Environment.GetEnvironmentVariable("PUPLAYER_TEST_ROOT")!;
        Directory.CreateDirectory(root);
        var cache = Path.Combine(root, $"{Guid.NewGuid():N}.mka");
        File.WriteAllBytes(cache, []);
        try
        {
            var mpv = new RecordingMpvClient();
            var worker = new PlayerWorker(mpv);

            await worker.ApplyAsync(new PlayerRequest.SetAudioFilter(1, "lavfi=[highpass=f=70]"), default);
            await worker.ApplyAsync(new PlayerRequest.LoadExternalAudio(2, cache), default);
            await worker.ApplyAsync(new PlayerRequest.UseOriginalAudio(3), default);

            Assert.Equal(["filter:lavfi=[highpass=f=70]", $"audio:{cache}", "audio:original"], mpv.Calls);
        }
        finally { File.Delete(cache); }
    }

    [Fact]
    public async Task CaptureFrame_ReturnsPixelsWithTheRequestId()
    {
        var worker = new PlayerWorker(new RecordingMpvClient());

        var result = await worker.ApplyAsync(new PlayerRequest.CaptureFrame(27), default);

        var frame = Assert.IsType<PlayerEvent.FrameCaptured>(result);
        Assert.Equal(27, frame.RequestId);
        Assert.Equal([1, 2, 3], frame.Frame.Rgb24);
    }

    private sealed class RecordingMpvClient : IMpvClient
    {
        public List<string> Calls { get; } = [];
        public bool Disposed { get; private set; }
        public void Load(string path) => Calls.Add($"load:{path}");
        public void SetPaused(bool value) => Calls.Add($"pause:{value}");
        public void Seek(double value) => Calls.Add($"seek:{value}");
        public void SetVolume(double value) => Calls.Add($"volume:{value}");
        public void SetSpeed(double value) => Calls.Add($"speed:{value}");
        public void SetTransform(MpvTransform value) => Calls.Add($"transform:{value}");
        public void SetGeometry(VideoTransform value) => Calls.Add($"geometry:{value}");
        public void SetAudioFilter(string value) => Calls.Add($"filter:{value}");
        public void LoadExternalAudio(string path) => Calls.Add($"audio:{path}");
        public void UseOriginalAudio() => Calls.Add("audio:original");
        public VideoFrame CaptureFrame(int maxWidth) => new(1, 1, [1, 2, 3]);
        public PlayerSnapshot ReadSnapshot() => new(0, 0, true, 1, 100);
        public void Dispose() => Disposed = true;
    }
}
