using System.Globalization;
using System.IO;
using PUPlayer.App.AudioProcessing;

namespace PUPlayer.App.MediaTools;

public sealed class FfmpegClipExportService(string ffmpegPath, IProcessRunner? runner = null) : IClipExportService
{
    private readonly IProcessRunner runner = runner ?? new ProcessRunner();
    private readonly Dictionary<SourceStamp, KeyframeScan> scans = [];
    private readonly SemaphoreSlim scanGate = new(1, 1);
    private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    public async Task<string> ExportAsync(ClipExportRequest request, IProgress<ClipExportProgress>? progress, CancellationToken cancellationToken)
    {
        var source = Path.GetFullPath(request.Source);
        var output = Path.GetFullPath(request.Output);
        if (!File.Exists(source)) throw new FileNotFoundException("No se encontró el video.", source);
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var partial = output + ".partial";
        if (File.Exists(partial)) File.Delete(partial);
        try
        {
            if (request.Mode == ClipExportMode.CurrentView)
                await EncodeSelection(request, partial, true, progress, cancellationToken);
            else
            {
                progress?.Report(new(.05, "Buscando fotogramas clave…"));
                var scan = await ScanAsync(source, cancellationToken);
                if (!scan.SupportsHybrid || !await TryHybridAsync(request, scan.Seconds, partial, progress, cancellationToken))
                    await EncodeSelection(request, partial, false, progress, cancellationToken);
            }
            if (!File.Exists(partial) || new FileInfo(partial).Length == 0) throw new InvalidDataException("FFmpeg no generó el clip.");
            File.Move(partial, output, false);
            progress?.Report(new(1, "Clip guardado"));
            return output;
        }
        finally { if (File.Exists(partial)) File.Delete(partial); }
    }

    private async Task<KeyframeScan> ScanAsync(string source, CancellationToken cancellationToken)
    {
        var file = new FileInfo(source);
        var stamp = new SourceStamp(source, file.Length, file.LastWriteTimeUtc.Ticks);
        await scanGate.WaitAsync(cancellationToken);
        try
        {
            if (scans.TryGetValue(stamp, out var cached)) return cached;
            var scan = await new FfmpegKeyframeScanner(ffmpegPath, runner).ScanAsync(source, cancellationToken);
            foreach (var old in scans.Keys.Where(x => x.Path.Equals(source, StringComparison.OrdinalIgnoreCase)).ToArray()) scans.Remove(old);
            scans[stamp] = scan;
            return scan;
        }
        finally { scanGate.Release(); }
    }

    private async Task<bool> TryHybridAsync(ClipExportRequest request, double[] keys, string partial,
        IProgress<ClipExportProgress>? progress, CancellationToken cancellationToken)
    {
        var startKey = keys.FirstOrDefault(x => x > request.Selection.Start);
        var endKey = keys.LastOrDefault(x => x < request.Selection.End);
        if (startKey <= request.Selection.Start || endKey <= startKey) return false;
        var temp = Path.Combine(request.Source + ".pucache", "export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var first = Path.Combine(temp, "first.ts");
        var middle = Path.Combine(temp, "middle.ts");
        var last = Path.Combine(temp, "last.ts");
        try
        {
            progress?.Report(new(.15, "Procesando bordes…"));
            await RunAsync(["-ss", N(request.Selection.Start), "-i", request.Source, "-t", N(startKey - request.Selection.Start),
                "-map", "0:v:0", "-map", "0:a?", "-c:v", "libx264", "-preset", "fast", "-crf", "18", "-c:a", "aac", "-b:a", "192k", "-f", "mpegts", first], first, cancellationToken);
            progress?.Report(new(.4, "Copiando tramo central…"));
            await RunAsync(["-ss", N(startKey), "-i", request.Source, "-t", N(endKey - startKey), "-map", "0:v:0", "-map", "0:a?",
                "-c", "copy", "-bsf:v", "h264_mp4toannexb", "-f", "mpegts", middle], middle, cancellationToken);
            await RunAsync(["-ss", N(endKey), "-i", request.Source, "-t", N(request.Selection.End - endKey),
                "-map", "0:v:0", "-map", "0:a?", "-c:v", "libx264", "-preset", "fast", "-crf", "18", "-c:a", "aac", "-b:a", "192k", "-f", "mpegts", last], last, cancellationToken);
            progress?.Report(new(.8, "Uniendo clip…"));
            await RunAsync(["-i", $"concat:{first}|{middle}|{last}", "-c", "copy", "-bsf:a", "aac_adtstoasc", "-movflags", "+faststart", "-f", "mp4", partial], partial, cancellationToken);
            return true;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            if (File.Exists(partial)) File.Delete(partial);
            return false;
        }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
    }

    private Task EncodeSelection(ClipExportRequest request, string partial, bool transformed,
        IProgress<ClipExportProgress>? progress, CancellationToken cancellationToken)
    {
        progress?.Report(new(.2, transformed ? "Aplicando vista al clip…" : "Codificando fragmento compatible…"));
        var args = new List<string> { "-ss", N(request.Selection.Start), "-i", Path.GetFullPath(request.Source), "-t", N(request.Selection.Duration), "-map", "0:v:0", "-map", "0:a?" };
        var filter = transformed ? request.Transform.ToFfmpegFilter() : "";
        if (filter.Length > 0) { args.Add("-vf"); args.Add(filter + ",scale=trunc(iw/2)*2:trunc(ih/2)*2"); }
        args.AddRange(["-c:v", "libx264", "-preset", "fast", "-crf", "18", "-c:a", "aac", "-b:a", "192k", "-movflags", "+faststart", "-f", "mp4", partial]);
        return RunAsync(args, partial, cancellationToken);
    }

    private async Task RunAsync(IReadOnlyList<string> arguments, string output, CancellationToken cancellationToken)
    {
        var args = new List<string> { "-y", "-hide_banner", "-loglevel", "error" };
        args.AddRange(arguments);
        var result = await runner.RunAsync(new(ffmpegPath, args), cancellationToken);
        if (result.ExitCode != 0 || !File.Exists(output) || new FileInfo(output).Length == 0)
            throw new InvalidOperationException($"FFmpeg falló: {result.StandardError.Trim()}");
    }

    private readonly record struct SourceStamp(string Path, long Length, long ModifiedUtcTicks);
}
