using PUPlayer.Core.Favorites;

namespace PUPlayer.Core.Tests.Favorites;

public sealed class FavoriteStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "IgoonTube-favorites-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void RoundTrip_LoadsWithoutSceneAnalysis()
    {
        var video = CreateVideo();
        var store = new FavoriteStore();

        store.Save(video, [44, 12.5, 12.5]);

        Assert.Equal([12.5, 44], store.Load(video).Seconds);
    }

    [Fact]
    public void ChangedSource_DoesNotReuseFavorites()
    {
        var video = CreateVideo();
        var store = new FavoriteStore();
        store.Save(video, [12.5]);
        File.AppendAllText(video, "changed");

        Assert.Empty(store.Load(video).Seconds);
    }

    [Fact]
    public void MissingOrDamagedIndex_ReturnsEmpty()
    {
        var video = CreateVideo();
        Directory.CreateDirectory(video + ".pucache");
        File.WriteAllText(Path.Combine(video + ".pucache", "favorites-v1.json"), "broken");

        Assert.Empty(new FavoriteStore().Load(video).Seconds);
    }

    private string CreateVideo()
    {
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "sample.mp4");
        File.WriteAllText(path, "video");
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
