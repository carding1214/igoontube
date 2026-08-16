using System.Text.Json;

namespace PUPlayer.Core.Favorites;

public sealed class FavoriteStore : IFavoriteStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = false };
    public string PathFor(string mediaPath) => Path.Combine(mediaPath + ".pucache", "favorites-v1.json");

    public FavoriteIndex Load(string mediaPath)
    {
        try
        {
            var value = JsonSerializer.Deserialize<FavoriteIndex>(File.ReadAllText(PathFor(mediaPath)), Options);
            return value?.Matches(mediaPath) == true ? value : FavoriteIndex.Empty;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return FavoriteIndex.Empty; }
    }

    public void Save(string mediaPath, IEnumerable<double> seconds)
    {
        var path = PathFor(mediaPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(FavoriteIndex.For(mediaPath, seconds), Options));
        File.Move(temporary, path, true);
    }
}
