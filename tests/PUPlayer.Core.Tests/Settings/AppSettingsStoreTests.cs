using PUPlayer.Core.Settings;

namespace PUPlayer.Core.Tests.Settings;

public sealed class AppSettingsStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "IgoonTube-settings-" + Guid.NewGuid().ToString("N"));
    private string PathName => Path.Combine(root, "settings.json");

    [Fact]
    public void CorruptJson_ReturnsDefaults()
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(PathName, "{");
        using var store = new AppSettingsStore(PathName);
        Assert.Equal(AppSettings.Default, store.Load());
    }

    [Fact]
    public void Save_IsAtomicAndReusableBySecondStore()
    {
        using var first = new AppSettingsStore(PathName);
        using var second = new AppSettingsStore(PathName);
        first.Save(AppSettings.Default with { Language = "es" });
        Assert.Equal("es", second.Load().Language);
        Assert.False(File.Exists(PathName + ".tmp"));
    }

    [Fact]
    public async Task Save_NotifiesAnotherStore()
    {
        using var first = new AppSettingsStore(PathName);
        using var second = new AppSettingsStore(PathName);
        var changed = new TaskCompletionSource<AppSettings>(TaskCreationOptions.RunContinuationsAsynchronously);
        second.Changed += value => changed.TrySetResult(value);

        first.Save(AppSettings.Default with { Language = "es" });

        var value = await changed.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Equal("es", value.Language);
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
