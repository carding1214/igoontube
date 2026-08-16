using PUPlayer.Core.Playback;

namespace PUPlayer.Core.Tests.Playback;

public sealed class LocalMediaPathTests
{
    [Fact]
    public void LocalMediaPath_AcceptsOnlyExistingAbsoluteFiles()
    {
        var root = Environment.GetEnvironmentVariable("PUPLAYER_TEST_ROOT")!;
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, $"{Guid.NewGuid():N}.tmp");
        File.WriteAllBytes(file, []);
        try { Assert.True(LocalMediaPath.TryCreate(file, out _)); }
        finally { File.Delete(file); }
        Assert.False(LocalMediaPath.TryCreate("missing.mp4", out _));
    }
}
