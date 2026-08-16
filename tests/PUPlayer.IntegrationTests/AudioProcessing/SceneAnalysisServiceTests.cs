using PUPlayer.App.AudioProcessing;
using PUPlayer.Core.Scenes;
using System.IO;

namespace PUPlayer.IntegrationTests.AudioProcessing;

public sealed class SceneAnalysisServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"igoontube-scenes-{Guid.NewGuid():N}");

    [Fact]
    public async Task Analyze_UsesVoiceCacheAndReusesSmallJsonIndex()
    {
        Directory.CreateDirectory(root);
        var media = Path.Combine(root, "video.mp4"); File.WriteAllBytes(media, [1, 2, 3]);
        var voice = Path.Combine(root, "voice.mka"); File.WriteAllBytes(voice, [4]);
        var runner = new FakeRunner(command => command.Arguments.Contains(voice)
            ? Stats(10, -20, -4)
            : Stats(20, -15, -8));
        var service = new SceneAnalysisService(new FakeSeparation(voice), runner, "ffmpeg", new SceneIndexStore());

        var first = await service.AnalyzeAsync(media, .7, default);
        var second = await service.AnalyzeAsync(media, .7, default);

        Assert.Contains(first.Markers, x => x.Kind == SceneMarkerKind.Voice && x.Seconds == 10);
        Assert.Contains(first.Markers, x => x.Kind == SceneMarkerKind.Detail && x.Seconds == 10);
        Assert.Contains(first.Markers, x => x.Kind == SceneMarkerKind.HighActivity && x.Seconds == 20);
        Assert.Equal(first.Markers, second.Markers);
        Assert.Equal(2, runner.Calls);
        Assert.True(new FileInfo(new SceneIndexStore().CachePath(media, .7)).Length < 4096);
    }

    private static ProcessResult Stats(double time, double rms, double peak) => new(0, "", $"""
        frame:1 pts:1 pts_time:{time}
        lavfi.astats.Overall.RMS_level={rms}
        lavfi.astats.Overall.Peak_level={peak}
        """);

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }

    private sealed class FakeRunner(Func<ProcessCommand, ProcessResult> result) : IProcessRunner
    {
        public int Calls { get; private set; }
        public Task<ProcessResult> RunAsync(ProcessCommand command, CancellationToken cancellationToken)
        { Calls++; return Task.FromResult(result(command)); }
    }

    private sealed class FakeSeparation(string voice) : IAudioSeparationService
    {
        public Task<string> GetOrCreateVoiceCacheAsync(string sourcePath, IProgress<AudioProcessingProgress>? progress, CancellationToken token) => Task.FromResult(voice);
        public Task<string> GetOrCreateDetailCacheAsync(string sourcePath, IProgress<AudioProcessingProgress>? progress, CancellationToken token) => Task.FromResult(voice);
    }
}
