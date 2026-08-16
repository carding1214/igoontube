using System.IO;

namespace PUPlayer.App.AudioProcessing;

public interface IAudioEncoder
{
    Task ExtractWavAsync(string sourceMedia, string outputWav, CancellationToken cancellationToken);
    Task EncodeAsync(string inputWav, string outputMka, CancellationToken cancellationToken);
    Task MixDetailAsync(string vocalsWav, string originalWav, string outputMka, CancellationToken cancellationToken);
}

public sealed class FfmpegAudioEncoder(string ffmpegPath, IProcessRunner? runner = null) : IAudioEncoder
{
    private readonly IProcessRunner runner = runner ?? new ProcessRunner();

    public async Task ExtractWavAsync(string sourceMedia, string outputWav, CancellationToken cancellationToken)
    {
        var result = await runner.RunAsync(new(ffmpegPath,
        [
            "-y", "-hide_banner", "-loglevel", "error", "-i", Path.GetFullPath(sourceMedia),
            "-map", "0:a:0", "-vn", "-ac", "2", "-ar", "44100", "-c:a", "pcm_s16le", outputWav
        ]), cancellationToken);
        if (result.ExitCode != 0) throw new InvalidOperationException($"FFmpeg failed: {result.StandardError.Trim()}");
    }

    public async Task EncodeAsync(string inputWav, string outputMka, CancellationToken cancellationToken)
    {
        if (!File.Exists(inputWav)) throw new FileNotFoundException("Input audio not found.", inputWav);
        if (!File.Exists(ffmpegPath)) throw new FileNotFoundException("FFmpeg not found.", ffmpegPath);
        var output = Path.GetFullPath(outputMka);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var partial = output + ".partial";
        if (File.Exists(partial)) File.Delete(partial);
        try
        {
            var result = await runner.RunAsync(new(ffmpegPath,
            [
                "-y", "-hide_banner", "-loglevel", "error", "-i", Path.GetFullPath(inputWav),
                "-map", "0:a:0", "-vn", "-c:a", "libopus", "-b:a", "64k", "-vbr", "on",
                "-application", "audio", "-f", "matroska", partial
            ]), cancellationToken);
            if (result.ExitCode != 0) throw new InvalidOperationException($"FFmpeg failed: {result.StandardError.Trim()}");
            if (!File.Exists(partial) || new FileInfo(partial).Length == 0) throw new InvalidDataException("FFmpeg produced no audio cache.");
            File.Move(partial, output, true);
        }
        finally
        {
            if (File.Exists(partial)) File.Delete(partial);
        }
    }

    public async Task MixDetailAsync(string vocalsWav, string originalWav, string outputMka, CancellationToken cancellationToken)
    {
        if (!File.Exists(vocalsWav) || !File.Exists(originalWav)) throw new FileNotFoundException("Audio input not found.");
        var output = Path.GetFullPath(outputMka);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var partial = output + ".partial";
        if (File.Exists(partial)) File.Delete(partial);
        try
        {
            const string filter = "[0:a]highpass=f=35,equalizer=f=3500:t=q:w=1:g=4,volume=1.35[v];" +
                                  "[1:a]volume=0.22[o];[v][o]amix=inputs=2:duration=longest:normalize=0," +
                                  "acompressor=threshold=0.18:ratio=2.5:attack=10:release=180:makeup=1.2,alimiter=limit=0.95[m]";
            var result = await runner.RunAsync(new(ffmpegPath,
            [
                "-y", "-hide_banner", "-loglevel", "error", "-i", Path.GetFullPath(vocalsWav),
                "-i", Path.GetFullPath(originalWav), "-filter_complex", filter, "-map", "[m]", "-vn",
                "-c:a", "libopus", "-b:a", "80k", "-vbr", "on", "-application", "audio", "-f", "matroska", partial
            ]), cancellationToken);
            if (result.ExitCode != 0) throw new InvalidOperationException($"FFmpeg failed: {result.StandardError.Trim()}");
            if (!File.Exists(partial) || new FileInfo(partial).Length == 0) throw new InvalidDataException("FFmpeg produced no audio cache.");
            File.Move(partial, output, true);
        }
        finally { if (File.Exists(partial)) File.Delete(partial); }
    }
}
