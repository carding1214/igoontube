using System.IO;
using PUPlayer.App.AudioProcessing;
using PUPlayer.App.MediaTools;

namespace PUPlayer.IntegrationTests.MediaTools;

public sealed class FfmpegThumbnailServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "IgoonTube-thumb-service-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task GeneratesNearestJpegAtomically()
    {
        Directory.CreateDirectory(root);
        var video = Path.Combine(root, "video.mp4");
        File.WriteAllText(video, "video");
        var runner = new RecordingRunner();

        var result = await new FfmpegThumbnailService("ffmpeg.exe", runner).GetAsync(video, 120, 45, default);

        Assert.True(File.Exists(result));
        Assert.False(File.Exists(result + ".partial"));
        Assert.Contains("-ss", runner.Command!.Arguments);
        Assert.Contains("scale=320:-2", runner.Command.Arguments);
        Assert.Contains("-frames:v", runner.Command.Arguments);
    }

    [Fact]
    public async Task ChangedSource_RegeneratesExistingThumbnail()
    {
        Directory.CreateDirectory(root);
        var video = Path.Combine(root, "video.mp4");
        File.WriteAllText(video, "video");
        var runner = new RecordingRunner();
        var service = new FfmpegThumbnailService("ffmpeg.exe", runner);
        await service.GetAsync(video, 120, 45, default);
        File.AppendAllText(video, "changed");

        await service.GetAsync(video, 120, 45, default);

        Assert.Equal(2, runner.CallCount);
    }

    private sealed class RecordingRunner : IProcessRunner
    {
        public ProcessCommand? Command { get; private set; }
        public int CallCount { get; private set; }
        public Task<ProcessResult> RunAsync(ProcessCommand command, CancellationToken cancellationToken)
        {
            Command = command;
            CallCount++;
            File.WriteAllBytes(command.Arguments[^1], [1]);
            return Task.FromResult(new ProcessResult(0, "", ""));
        }
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
