using System.IO;
using System.Globalization;
using PUPlayer.App.AudioProcessing;
using PUPlayer.Core.Thumbnails;

namespace PUPlayer.App.MediaTools;

public sealed class FfmpegThumbnailService(string ffmpegPath, IProcessRunner? runner = null) : IThumbnailService
{
    private readonly IProcessRunner runner = runner ?? new ProcessRunner();

    public async Task<string> GetAsync(string source, double duration, double seconds, CancellationToken cancellationToken)
    {
        var cache = new ThumbnailCache(source, duration);
        if (!cache.HasValidManifest)
        {
            if (Directory.Exists(cache.DirectoryPath))
                foreach (var file in Directory.EnumerateFiles(cache.DirectoryPath)
                             .Where(x => Path.GetExtension(x).Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                         Path.GetFileName(x) is "manifest.json" or "manifest.json.tmp" ||
                                         x.EndsWith(".partial", StringComparison.OrdinalIgnoreCase)))
                    File.Delete(file);
            cache.SaveManifest();
        }
        var timestamp = cache.NearestTimestamp(seconds);
        var output = cache.PathFor(timestamp);
        if (File.Exists(output) && new FileInfo(output).Length > 0) return output;
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var partial = output + ".partial";
        try
        {
            var result = await runner.RunAsync(new(ffmpegPath,
            [
                "-y", "-hide_banner", "-loglevel", "error", "-ss", timestamp.ToString("0.###", CultureInfo.InvariantCulture),
                "-i", Path.GetFullPath(source), "-frames:v", "1", "-vf", "scale=320:-2", "-q:v", "5", "-f", "image2", partial
            ]), cancellationToken);
            if (result.ExitCode != 0) throw new InvalidOperationException($"FFmpeg falló: {result.StandardError.Trim()}");
            if (!File.Exists(partial) || new FileInfo(partial).Length == 0) throw new InvalidDataException("FFmpeg no generó miniatura.");
            File.Move(partial, output, true);
            return output;
        }
        finally { if (File.Exists(partial)) File.Delete(partial); }
    }
}
