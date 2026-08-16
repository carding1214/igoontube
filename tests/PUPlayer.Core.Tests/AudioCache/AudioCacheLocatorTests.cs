using PUPlayer.Core.AudioCache;

namespace PUPlayer.Core.Tests.AudioCache;

public sealed class AudioCacheLocatorTests
{
    [Fact]
    public void Key_IsDeterministicAndInvalidatesWhenSourceChanges()
    {
        using var files = TestFiles.Create();
        var source = files.Write("clip.mp4", [1, 2, 3]);
        File.SetLastWriteTimeUtc(source, new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc));

        var first = AudioCacheKey.FromFile(source, "htdemucs");
        var same = AudioCacheKey.FromFile(source, "htdemucs");
        File.AppendAllText(source, "x");
        var changed = AudioCacheKey.FromFile(source, "htdemucs");

        Assert.Equal(first.Hash, same.Hash);
        Assert.NotEqual(first.Hash, changed.Hash);
    }

    [Fact]
    public void Locator_UsesAdjacentCacheOutsideC()
    {
        using var files = TestFiles.Create();
        var source = files.Write("clip.mp4", [1]);
        var key = AudioCacheKey.FromFile(source, "htdemucs");

        var result = new AudioCacheLocator(files.Path("fallback")).Locate(key);

        Assert.Equal(System.IO.Path.Combine(source + ".pucache", $"voice-{key.Hash}.mka"), result.AudioPath);
    }

    [Fact]
    public void Locator_UsesFRootForCSource()
    {
        using var files = TestFiles.Create();
        var key = AudioCacheKey.Create(@"C:\Media\clip.mp4", 42, DateTime.UnixEpoch, "htdemucs");

        var result = new AudioCacheLocator(files.Path("fallback")).Locate(key);

        Assert.Equal(files.Path("fallback", key.Hash, "voice.mka"), result.AudioPath);
    }

    [Fact]
    public void Locator_UsesFallbackWhenAdjacentDirectoryCannotBeCreated()
    {
        using var files = TestFiles.Create();
        var source = files.Write("clip.mp4", [1]);
        File.WriteAllBytes(source + ".pucache", [1]);
        var key = AudioCacheKey.FromFile(source, "htdemucs");

        var result = new AudioCacheLocator(files.Path("fallback")).Locate(key);

        Assert.Equal(files.Path("fallback", key.Hash, "voice.mka"), result.AudioPath);
    }

    [Fact]
    public void Manifest_AcceptsOnlyMatchingAudioAndLeavesNoPartialFile()
    {
        using var files = TestFiles.Create();
        var source = files.Write("clip.mp4", [1]);
        var audio = files.Write("voice.mka", [1, 2, 3]);
        var path = files.Path("manifest.json");
        var key = AudioCacheKey.FromFile(source, "htdemucs");

        AudioCacheManifest.From(key, 3).SaveAtomic(path);
        var loaded = AudioCacheManifest.Load(path);

        Assert.True(loaded.Matches(key, audio));
        File.AppendAllText(audio, "x");
        Assert.False(loaded.Matches(key, audio));
        Assert.Empty(Directory.GetFiles(files.Root, "*.partial-*"));
    }

    private sealed class TestFiles : IDisposable
    {
        public string Root { get; }
        private TestFiles(string root) { Root = root; Directory.CreateDirectory(root); }
        public static TestFiles Create() => new(System.IO.Path.Combine(
            Environment.GetEnvironmentVariable("PUPLAYER_TEST_ROOT")!, Guid.NewGuid().ToString("N")));
        public string Path(params string[] parts) => parts.Aggregate(Root, System.IO.Path.Combine);
        public string Write(string name, byte[] bytes) { var path = Path(name); File.WriteAllBytes(path, bytes); return path; }
        public void Dispose() => Directory.Delete(Root, true);
    }
}
