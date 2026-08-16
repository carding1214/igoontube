using System.Globalization;
using PUPlayer.Core.Scenes;

namespace PUPlayer.App.AudioProcessing;

public interface ISceneAnalysisService
{
    Task<SceneIndex> AnalyzeAsync(string mediaPath, double sensitivity, CancellationToken cancellationToken);
}

public sealed class SceneAnalysisService(
    IAudioSeparationService separation,
    IProcessRunner runner,
    string ffmpegPath,
    SceneIndexStore store) : ISceneAnalysisService
{
    public async Task<SceneIndex> AnalyzeAsync(string mediaPath, double sensitivity, CancellationToken cancellationToken)
    {
        sensitivity = Math.Clamp(sensitivity, 0, 1);
        if (store.Load(mediaPath, sensitivity) is { } cached) return cached;

        var voicePath = await separation.GetOrCreateVoiceCacheAsync(mediaPath, null, cancellationToken);
        var voice = await ReadStatsAsync(voicePath, cancellationToken);
        var original = await ReadStatsAsync(mediaPath, cancellationToken);
        var threshold = -50 + sensitivity * 25;
        var markers = new List<SceneMarker>();
        markers.AddRange(voice.Where(x => x.Rms >= threshold).Select(x => new SceneMarker(x.Time, SceneMarkerKind.Voice, "Voz")));
        markers.AddRange(voice.Where(x => x.Rms >= threshold - 10 && x.Peak - x.Rms >= 10).Select(x => new SceneMarker(x.Time, SceneMarkerKind.Detail, "Detalle")));
        markers.AddRange(original.Where(x => x.Rms >= threshold).Select(x => new SceneMarker(x.Time, SceneMarkerKind.HighActivity, "Actividad alta")));
        var index = SceneIndex.For(mediaPath, sensitivity, Deduplicate(markers));
        store.Save(mediaPath, index);
        return index;
    }

    private async Task<IReadOnlyList<Stat>> ReadStatsAsync(string path, CancellationToken token)
    {
        var result = await runner.RunAsync(new(ffmpegPath,
            ["-hide_banner", "-nostdin", "-i", path, "-vn", "-af", "astats=metadata=1:reset=1,ametadata=print", "-f", "null", "-"]), token);
        if (result.ExitCode != 0) throw new InvalidOperationException("No se pudo analizar el audio del video.");
        return Parse(result.StandardOutput + '\n' + result.StandardError);
    }

    internal static IReadOnlyList<Stat> Parse(string text)
    {
        var result = new List<Stat>();
        double? time = null, rms = null, peak = null;
        foreach (var line in text.Split('\n'))
        {
            if (ValueAfter(line, "pts_time:") is { } t)
            {
                Add(); time = t; rms = peak = null;
            }
            else if (ValueAfter(line, "lavfi.astats.Overall.RMS_level=") is { } r) rms = r;
            else if (ValueAfter(line, "lavfi.astats.Overall.Peak_level=") is { } p) { peak = p; Add(); }
        }
        Add();
        return result;

        void Add()
        {
            if (time is { } t && rms is { } r && peak is { } p) result.Add(new(t, r, p));
        }
    }

    private static double? ValueAfter(string line, string key)
    {
        var index = line.IndexOf(key, StringComparison.Ordinal);
        if (index < 0) return null;
        var value = line[(index + key.Length)..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static IEnumerable<SceneMarker> Deduplicate(IEnumerable<SceneMarker> markers) =>
        markers.OrderBy(x => x.Seconds).ThenBy(x => x.Kind)
            .GroupBy(x => x.Kind)
            .SelectMany(group => group.Aggregate(new List<SceneMarker>(), (kept, marker) =>
            { if (kept.Count == 0 || marker.Seconds - kept[^1].Seconds >= 3) kept.Add(marker); return kept; }))
            .OrderBy(x => x.Seconds);

    internal readonly record struct Stat(double Time, double Rms, double Peak);
}
