using PUPlayer.Core.Cache;

namespace PUPlayer.Core.Tests.Cache;

public sealed class IgoonTubeCacheManagerTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "IgoonTube-cache-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Scan_ReportsOnlyOwnedFiles()
    {
        var video = Prepare();
        File.WriteAllBytes(Path.Combine(video + ".pucache", "voice-a.mka"), new byte[10]);
        File.WriteAllBytes(Path.Combine(video + ".pucache", "notes.txt"), new byte[20]);

        var report = new IgoonTubeCacheManager(root).ScanVideo(video);

        Assert.Equal(10, report.TotalBytes);
    }

    [Fact]
    public void DeleteVideoCache_PreservesFavoritesAndUnknownFiles()
    {
        var video = Prepare();
        var cache = video + ".pucache";
        File.WriteAllText(Path.Combine(cache, "favorites-v1.json"), "favorite");
        File.WriteAllText(Path.Combine(cache, "notes.txt"), "unknown");
        File.WriteAllText(Path.Combine(cache, "scenes-hybrid-v1-0.70.json"), "scene");

        new IgoonTubeCacheManager(root).DeleteVideo(video, CacheCategory.All);

        Assert.True(File.Exists(Path.Combine(cache, "favorites-v1.json")));
        Assert.True(File.Exists(Path.Combine(cache, "notes.txt")));
        Assert.False(File.Exists(Path.Combine(cache, "scenes-hybrid-v1-0.70.json")));
    }

    [Fact]
    public void DeleteThumbnails_LeavesAudioCaches()
    {
        var video = Prepare();
        var cache = video + ".pucache";
        Directory.CreateDirectory(Path.Combine(cache, "thumbnails-v1"));
        File.WriteAllBytes(Path.Combine(cache, "thumbnails-v1", "000001.jpg"), [1]);
        File.WriteAllBytes(Path.Combine(cache, "voice-a.mka"), [1]);

        new IgoonTubeCacheManager(root).DeleteVideo(video, CacheCategory.Thumbnails);

        Assert.True(File.Exists(Path.Combine(cache, "voice-a.mka")));
        Assert.False(File.Exists(Path.Combine(cache, "thumbnails-v1", "000001.jpg")));
    }

    [Fact]
    public void Delete_ReturnsFailuresWithoutCountingThem()
    {
        var video = Prepare();
        var cache = video + ".pucache";
        var deleted = Path.Combine(cache, "voice-ok.mka");
        var blocked = Path.Combine(cache, "voice-blocked.mka");
        File.WriteAllBytes(deleted, new byte[10]);
        File.WriteAllBytes(blocked, new byte[20]);
        var manager = new IgoonTubeCacheManager(root, path =>
        {
            if (path == blocked) throw new IOException("locked");
            File.Delete(path);
        });

        var result = manager.DeleteVideo(video, CacheCategory.Audio);

        Assert.Equal(10, result.FreedBytes);
        Assert.Equal(1, result.DeletedFiles);
        Assert.Equal([blocked], result.FailedFiles);
    }

    private string Prepare()
    {
        Directory.CreateDirectory(root);
        var video = Path.Combine(root, "video.mp4");
        File.WriteAllText(video, "video");
        Directory.CreateDirectory(video + ".pucache");
        return video;
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
