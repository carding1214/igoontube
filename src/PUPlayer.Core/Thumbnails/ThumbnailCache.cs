using System.Text.Json;
using PUPlayer.Core.MediaTools;

namespace PUPlayer.Core.Thumbnails;

public sealed class ThumbnailCache
{
    private readonly string source;
    private readonly double duration;
    private readonly ThumbnailPlan plan;

    public ThumbnailCache(string source, double duration)
    {
        this.source = Path.GetFullPath(source);
        this.duration = duration;
        plan = ThumbnailPlan.ForDuration(duration);
    }

    public string DirectoryPath => Path.Combine(source + ".pucache", "thumbnails-v1");
    public string ManifestPath => Path.Combine(DirectoryPath, "manifest.json");
    public bool HasValidManifest
    {
        get
        {
            try
            {
                var value = JsonSerializer.Deserialize<ThumbnailManifest>(File.ReadAllText(ManifestPath));
                return value?.Matches(source, duration, plan.Count) == true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return false; }
        }
    }

    public double NearestTimestamp(double seconds) =>
        Math.Clamp(Math.Round(Math.Clamp(seconds, 0, duration) / plan.Interval) * plan.Interval, 0, duration);

    public string PathFor(double seconds)
    {
        var index = plan.Interval > 0 ? (int)Math.Round(NearestTimestamp(seconds) / plan.Interval) : 0;
        return Path.Combine(DirectoryPath, $"{index:000000}.jpg");
    }

    public void SaveManifest()
    {
        Directory.CreateDirectory(DirectoryPath);
        var temporary = ManifestPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(ThumbnailManifest.For(source, duration, plan.Count)));
        File.Move(temporary, ManifestPath, true);
    }
}
