using System.IO;
using PUPlayer.App.AudioProcessing;
using PUPlayer.App.MediaTools;
using PUPlayer.Core.MediaTools;

namespace PUPlayer.IntegrationTests.MediaTools;

public sealed class RealMediaToolsSmokeTests
{
    [Fact]
    public async Task SuppliedVideo_ExportsHybridAndTransformedClips()
    {
        var source = Environment.GetEnvironmentVariable("IGOONTUBE_REAL_VIDEO");
        if (string.IsNullOrWhiteSpace(source) || !File.Exists(source)) return;
        var root = Path.Combine(Environment.GetEnvironmentVariable("PUPLAYER_TEST_ROOT")!, "real-media-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var ffmpeg = FindRepoFile("ffmpeg.exe");
        try
        {
            var service = new FfmpegClipExportService(ffmpeg);
            var thumbnail = await new FfmpegThumbnailService(ffmpeg).GetAsync(source, 1983.14, 100, default);
            var original = Path.Combine(root, "original.mp4");
            var transformed = Path.Combine(root, "transformed.mp4");

            await service.ExportAsync(new(source, new(60, 80), original, ClipExportMode.Original, new()), null, default);
            await service.ExportAsync(new(source, new(90, 100), transformed, ClipExportMode.CurrentView, new(90, true)), null, default);

            foreach (var output in new[] { original, transformed })
            {
                var result = await new ProcessRunner().RunAsync(new(ffmpeg, ["-v", "error", "-i", output, "-f", "null", "NUL"]), default);
                Assert.Equal(0, result.ExitCode);
                Assert.True(new FileInfo(output).Length > 100_000);
                Assert.False(File.Exists(output + ".partial"));
            }
            Assert.True(new FileInfo(thumbnail).Length > 1_000);
        }
        finally { Directory.Delete(root, true); }
    }

    private static string FindRepoFile(string name)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, name);
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException(name);
    }
}
