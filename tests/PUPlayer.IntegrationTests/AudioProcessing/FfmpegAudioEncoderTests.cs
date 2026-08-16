using System.IO;
using System.Text;
using PUPlayer.App.AudioProcessing;

namespace PUPlayer.IntegrationTests.AudioProcessing;

public sealed class FfmpegAudioEncoderTests
{
    [Fact]
    public async Task EncodeAsync_CreatesSmallOpusAudioWithoutVideo()
    {
        var root = Path.Combine(Environment.GetEnvironmentVariable("PUPLAYER_TEST_ROOT")!, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var input = Path.Combine(root, "voice.wav");
            var output = Path.Combine(root, "voice.mka");
            WriteWave(input);
            var ffmpeg = FindRepoFile("ffmpeg.exe");

            await new FfmpegAudioEncoder(ffmpeg).EncodeAsync(input, output, default);
            var probe = await new ProcessRunner().RunAsync(
                new ProcessCommand(ffmpeg, ["-hide_banner", "-i", output, "-f", "null", "-"]), default);

            Assert.Equal(0, probe.ExitCode);
            Assert.Contains("Audio: opus", probe.StandardError);
            Assert.DoesNotContain("Video:", probe.StandardError);
            Assert.InRange(new FileInfo(output).Length, 1, 200_000);
            Assert.False(File.Exists(output + ".partial"));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task MixDetailAsync_CreatesLimitedAudioOnlyCache()
    {
        var root = Path.Combine(Environment.GetEnvironmentVariable("PUPLAYER_TEST_ROOT")!, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var vocals = Path.Combine(root, "vocals.wav");
            var original = Path.Combine(root, "original.wav");
            var output = Path.Combine(root, "detail.mka");
            WriteWave(vocals); WriteWave(original);
            var ffmpeg = FindRepoFile("ffmpeg.exe");

            await new FfmpegAudioEncoder(ffmpeg).MixDetailAsync(vocals, original, output, default);
            var probe = await new ProcessRunner().RunAsync(new(ffmpeg, ["-hide_banner", "-i", output, "-f", "null", "-"]), default);

            Assert.Equal(0, probe.ExitCode);
            Assert.Contains("Audio: opus", probe.StandardError);
            Assert.DoesNotContain("Video:", probe.StandardError);
        }
        finally { Directory.Delete(root, true); }
    }

    private static void WriteWave(string path)
    {
        const int rate = 16_000;
        const int size = rate * 2;
        using var writer = new BinaryWriter(File.Create(path), Encoding.ASCII);
        writer.Write("RIFF"u8); writer.Write(36 + size); writer.Write("WAVE"u8);
        writer.Write("fmt "u8); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
        writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(size); writer.Write(new byte[size]);
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
