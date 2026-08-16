namespace PUPlayer.Core.Thumbnails;

public sealed record ThumbnailManifest(long Length, long ModifiedUtcTicks, double Duration, int Count)
{
    public static ThumbnailManifest For(string source, double duration, int count)
    {
        var file = new FileInfo(source);
        return new(file.Length, file.LastWriteTimeUtc.Ticks, duration, count);
    }

    public bool Matches(string source, double duration, int count)
    {
        var file = new FileInfo(source);
        return file.Exists && file.Length == Length && file.LastWriteTimeUtc.Ticks == ModifiedUtcTicks &&
               Math.Abs(Duration - duration) < .01 && Count == count;
    }
}
