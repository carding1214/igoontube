using PUPlayer.App.ViewModels;
using PUPlayer.Core.Workspace;
using PUPlayer.IntegrationTests.Fakes;
using PUPlayer.App.Features;
using PUPlayer.Core.Cache;

namespace PUPlayer.IntegrationTests.Workspace;

public sealed class WorkspaceViewModelTests
{
    [Fact]
    public async Task DropSecondFile_CreatesHorizontalSplit()
    {
        var vm = new WorkspaceViewModel(() => new FakePlayerBackend());

        await vm.OpenAsync(@"F:\media\one.mp4");
        await vm.DropAsync(@"F:\media\two.mp4");

        Assert.Equal(LayoutMode.SplitHorizontal, vm.Layout);
        Assert.Equal(2, vm.Panels.Count);
    }

    [Fact]
    public async Task DropThirdFile_RequiresExplicitReplacement()
    {
        var vm = new WorkspaceViewModel(() => new FakePlayerBackend());
        await vm.OpenAsync("a");
        await vm.DropAsync("b");

        Assert.Equal(DropResult.ReplaceChoiceRequired, await vm.DropAsync("c"));
    }

    [Fact]
    public async Task ActivatePrivacy_SilencesAndHidesBothPanels()
    {
        var backends = new Queue<FakePlayerBackend>([new(), new()]);
        var vm = new WorkspaceViewModel(() => backends.Dequeue());
        await vm.OpenAsync("one.mp4");
        await vm.DropAsync("two.mp4");

        await vm.ActivatePrivacyAsync();

        Assert.True(vm.IsPrivate);
        Assert.All(vm.Panels, panel => Assert.Equal("Video privado", panel.DisplayName));
        Assert.All(vm.Panels, panel => Assert.Equal(0, panel.VolumePercent));
    }

    [Fact]
    public async Task EachPanel_GetsIndependentFeatureFactories()
    {
        var calls = 0;
        var vm = new WorkspaceViewModel(() => new FakePlayerBackend(), featureFactory: () =>
        {
            calls++;
            return new PlayerFeatureFactories(() => null, () => null, () => null, () => null, () => null);
        });

        await vm.OpenAsync("one.mp4");
        await vm.DropAsync("two.mp4");

        Assert.Equal(2, calls);
    }

    [Fact]
    public void GlobalCache_IsScannedOnlyWhenRequested()
    {
        var cache = new CountingCacheCatalog();
        var vm = new WorkspaceViewModel(() => new FakePlayerBackend(), cacheManager: cache);

        vm.Relocalize();
        Assert.Equal(0, cache.GlobalScans);
        Assert.Equal("Caché", vm.GlobalCacheStatus);

        vm.RefreshGlobalCache();
        Assert.Equal(1, cache.GlobalScans);
    }

    private sealed class CountingCacheCatalog : ICacheCatalog
    {
        public int GlobalScans { get; private set; }
        public CacheReport ScanVideo(string mediaPath) => new([]);
        public CacheReport ScanGlobal() { GlobalScans++; return new([]); }
        public CacheDeleteResult DeleteVideo(string mediaPath, CacheCategory categories) => new(0, 0, []);
        public CacheDeleteResult DeleteGlobal(CacheCategory categories) => new(0, 0, []);
    }
}
