namespace PUPlayer.Core.Favorites;

public sealed record FavoriteIndex(long Length, long ModifiedUtcTicks, string Version, double[] Seconds)
{
    public const string CurrentVersion = "favorites-v1";
    public static FavoriteIndex Empty { get; } = new(0, 0, CurrentVersion, []);

    public static FavoriteIndex For(string mediaPath, IEnumerable<double> seconds)
    {
        var file = new FileInfo(mediaPath);
        return new(file.Length, file.LastWriteTimeUtc.Ticks, CurrentVersion,
            seconds.Where(x => x >= 0).Distinct().Order().ToArray());
    }

    public bool Matches(string mediaPath)
    {
        var file = new FileInfo(mediaPath);
        return file.Exists && file.Length == Length && file.LastWriteTimeUtc.Ticks == ModifiedUtcTicks && Version == CurrentVersion;
    }
}
