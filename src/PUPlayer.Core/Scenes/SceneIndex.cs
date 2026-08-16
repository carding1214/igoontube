namespace PUPlayer.Core.Scenes;

public sealed record SceneIndex(long Length, long ModifiedUtcTicks, string Version, double Sensitivity, SceneMarker[] Markers)
{
    public const string CurrentVersion = "hybrid-v1";
    public static SceneIndex For(string mediaPath, double sensitivity, IEnumerable<SceneMarker> markers)
    {
        var file = new FileInfo(mediaPath);
        return new(file.Length, file.LastWriteTimeUtc.Ticks, CurrentVersion, sensitivity, markers.ToArray());
    }

    public bool Matches(string mediaPath, double sensitivity)
    {
        var file = new FileInfo(mediaPath);
        return file.Exists && file.Length == Length && file.LastWriteTimeUtc.Ticks == ModifiedUtcTicks &&
               Version == CurrentVersion && Math.Abs(Sensitivity - sensitivity) < .0001;
    }
}
