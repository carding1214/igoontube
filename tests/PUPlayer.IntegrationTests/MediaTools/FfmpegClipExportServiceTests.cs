using System.IO;
using PUPlayer.App.AudioProcessing;
using PUPlayer.App.MediaTools;
using PUPlayer.Core.MediaTools;

namespace PUPlayer.IntegrationTests.MediaTools;

public sealed class FfmpegClipExportServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "IgoonTube-export-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task OriginalMode_UsesHybridAroundKeyframes()
    {
        var (video, output) = Files();
        var runner = new ExportRunner("Video: h264\nAudio: aac\npts_time:0\npts_time:10\npts_time:20\npts_time:30");

        await new FfmpegClipExportService("ffmpeg.exe", runner).ExportAsync(
            new(video, new(3, 27), output, ClipExportMode.Original, new()), null, default);

        Assert.Contains(runner.Commands, x => x.Arguments.Contains("-c") && x.Arguments.Contains("copy"));
        Assert.Contains(runner.Commands, x => x.Arguments.Any(a => a.StartsWith("concat:", StringComparison.Ordinal)));
        Assert.True(File.Exists(output));
    }

    [Fact]
    public async Task TransformedMode_ReencodesOnlySelection()
    {
        var (video, output) = Files();
        var runner = new ExportRunner("");

        await new FfmpegClipExportService("ffmpeg.exe", runner).ExportAsync(
            new(video, new(3, 27), output, ClipExportMode.CurrentView, new(90, true)), null, default);

        var command = Assert.Single(runner.Commands);
        Assert.Contains("-ss", command.Arguments);
        Assert.Contains("-t", command.Arguments);
        Assert.Contains(command.Arguments, x => x.Contains("hflip,transpose=1", StringComparison.Ordinal));
        Assert.DoesNotContain("copy", command.Arguments);
    }

    [Fact]
    public async Task UnsupportedInput_FallsBackToSelectionOnlyEncode()
    {
        var (video, output) = Files();
        var runner = new ExportRunner("Video: vp9\nAudio: opus\npts_time:0\npts_time:10\npts_time:20");

        await new FfmpegClipExportService("ffmpeg.exe", runner).ExportAsync(
            new(video, new(3, 17), output, ClipExportMode.Original, new()), null, default);

        Assert.Equal(2, runner.Commands.Count);
        Assert.Contains("libx264", runner.Commands[1].Arguments);
    }

    [Fact]
    public async Task RepeatedExport_ReusesKeyframeScanWhileSourceIsUnchanged()
    {
        var (video, output) = Files();
        var runner = new ExportRunner("Video: h264\nAudio: aac\npts_time:0\npts_time:10\npts_time:20\npts_time:30");
        var service = new FfmpegClipExportService("ffmpeg.exe", runner);

        await service.ExportAsync(new(video, new(3, 27), output, ClipExportMode.Original, new()), null, default);
        await service.ExportAsync(new(video, new(3, 27), Path.Combine(root, "second.mp4"), ClipExportMode.Original, new()), null, default);

        Assert.Single(runner.Commands, x => x.Arguments.Contains("showinfo"));
    }

    private (string Video, string Output) Files()
    {
        Directory.CreateDirectory(root);
        var video = Path.Combine(root, "video.mp4");
        File.WriteAllText(video, "video");
        return (video, Path.Combine(root, "video_clip_001.mp4"));
    }

    private sealed class ExportRunner(string scan) : IProcessRunner
    {
        public List<ProcessCommand> Commands { get; } = [];
        public Task<ProcessResult> RunAsync(ProcessCommand command, CancellationToken cancellationToken)
        {
            Commands.Add(command);
            if (command.Arguments.Contains("showinfo")) return Task.FromResult(new ProcessResult(0, "", scan));
            var output = command.Arguments[^1];
            if (!output.Equals("NUL", StringComparison.OrdinalIgnoreCase)) File.WriteAllBytes(output, [1]);
            return Task.FromResult(new ProcessResult(0, "", ""));
        }
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
