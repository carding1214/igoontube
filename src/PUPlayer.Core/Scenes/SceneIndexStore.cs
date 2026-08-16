using System.Globalization;
using System.Text.Json;

namespace PUPlayer.Core.Scenes;

public sealed class SceneIndexStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };
    public string CacheDirectory(string mediaPath) => mediaPath + ".pucache";
    public string CachePath(string mediaPath, double sensitivity) => Path.Combine(CacheDirectory(mediaPath),
        $"scenes-{SceneIndex.CurrentVersion}-{sensitivity.ToString("0.00", CultureInfo.InvariantCulture)}.json");

    public SceneIndex? Load(string mediaPath, double sensitivity)
    {
        try
        {
            var index = JsonSerializer.Deserialize<SceneIndex>(File.ReadAllText(CachePath(mediaPath, sensitivity)), Options);
            return index?.Matches(mediaPath, sensitivity) == true ? index : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return null; }
    }

    public void Save(string mediaPath, SceneIndex index)
    {
        var path = CachePath(mediaPath, index.Sensitivity);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(index, Options));
        File.Move(temporary, path, true);
    }
}
