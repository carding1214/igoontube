using PUPlayer.Core.Thumbnails;

namespace PUPlayer.Core.Tests.Thumbnails;

public sealed class ThumbnailCacheTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "IgoonTube-thumbs-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Timestamp_UsesAutomaticGrid()
    {
        var video = CreateVideo();
        var cache = new ThumbnailCache(video, 7200);

        Assert.Equal(240, cache.NearestTimestamp(257));
        Assert.EndsWith(Path.Combine("thumbnails-v1", "000006.jpg"), cache.PathFor(240));
    }

    [Fact]
    public void Manifest_InvalidatesWhenSourceChanges()
    {
        var video = CreateVideo();
        var cache = new ThumbnailCache(video, 120);
        cache.SaveManifest();
        File.AppendAllText(video, "changed");

        Assert.False(new ThumbnailCache(video, 120).HasValidManifest);
    }

    private string CreateVideo()
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "sample.mp4");
        File.WriteAllText(path, "video");
        return path;
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
