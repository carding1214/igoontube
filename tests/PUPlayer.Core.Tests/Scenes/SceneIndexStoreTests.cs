using PUPlayer.Core.Scenes;

namespace PUPlayer.Core.Tests.Scenes;

public sealed class SceneIndexStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "IgoonTube-scenes-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveAndLoad_RoundTripsValidIndex()
    {
        Directory.CreateDirectory(root);
        var media = Path.Combine(root, "video.mp4"); File.WriteAllBytes(media, [1, 2, 3]);
        var store = new SceneIndexStore();
        var index = SceneIndex.For(media, .7, [new(12.5, SceneMarkerKind.Detail, "Detalle")]);
        store.Save(media, index);
        Assert.Single(store.Load(media, .7)!.Markers);
    }

    [Fact]
    public void Load_InvalidatesChangedMedia()
    {
        Directory.CreateDirectory(root);
        var media = Path.Combine(root, "video.mp4"); File.WriteAllBytes(media, [1]);
        var store = new SceneIndexStore(); store.Save(media, SceneIndex.For(media, .5, []));
        File.AppendAllText(media, "changed");
        Assert.Null(store.Load(media, .5));
    }

    [Fact]
    public void Load_IgnoresCorruptJson()
    {
        Directory.CreateDirectory(root);
        var media = Path.Combine(root, "video.mp4"); File.WriteAllBytes(media, [1]);
        var store = new SceneIndexStore(); Directory.CreateDirectory(store.CacheDirectory(media));
        File.WriteAllText(store.CachePath(media, .5), "{");
        Assert.Null(store.Load(media, .5));
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
