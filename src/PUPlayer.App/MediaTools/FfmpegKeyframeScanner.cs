using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using PUPlayer.App.AudioProcessing;

namespace PUPlayer.App.MediaTools;

public sealed record KeyframeScan(double[] Seconds, bool SupportsHybrid);

public sealed partial class FfmpegKeyframeScanner(string ffmpegPath, IProcessRunner runner)
{
    public async Task<KeyframeScan> ScanAsync(string source, CancellationToken cancellationToken)
    {
        var result = await runner.RunAsync(new(ffmpegPath,
            ["-hide_banner", "-skip_frame", "nokey", "-i", Path.GetFullPath(source), "-vf", "showinfo", "-an", "-f", "null", "NUL"]), cancellationToken);
        if (result.ExitCode != 0) throw new InvalidOperationException($"No se pudieron leer fotogramas clave: {result.StandardError.Trim()}");
        var seconds = KeyframeRegex().Matches(result.StandardError)
            .Select(x => double.Parse(x.Groups[1].Value, CultureInfo.InvariantCulture)).Distinct().Order().ToArray();
        var compatible = result.StandardError.Contains("Video: h264", StringComparison.OrdinalIgnoreCase) &&
                         (!result.StandardError.Contains("Audio:", StringComparison.OrdinalIgnoreCase) ||
                          result.StandardError.Contains("Audio: aac", StringComparison.OrdinalIgnoreCase));
        return new(seconds, compatible);
    }

    [GeneratedRegex(@"pts_time:([0-9]+(?:\.[0-9]+)?)", RegexOptions.CultureInvariant)]
    private static partial Regex KeyframeRegex();
}
