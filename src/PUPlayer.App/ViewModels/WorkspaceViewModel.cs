using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using PUPlayer.App.Playback;
using PUPlayer.App.AudioProcessing;
using PUPlayer.Core.Workspace;
using PUPlayer.App.Tracking;
using PUPlayer.Core.Scenes;
using PUPlayer.Core.Favorites;
using PUPlayer.App.MediaTools;
using PUPlayer.Core.Cache;
using PUPlayer.App.Personalization;
using PUPlayer.App.Features;
using PUPlayer.Core.Playback;

namespace PUPlayer.App.ViewModels;

public enum DropResult { Added, ReplaceChoiceRequired }

public sealed class WorkspaceViewModel : ObservableObject, IAsyncDisposable
{
    private readonly Func<IPlayerBackend> backendFactory;
    private readonly IAudioSeparationService? audioSeparation;
    private readonly IVisionDetector? visionDetector;
    private readonly ISceneAnalysisService? sceneAnalysis;
    private readonly SceneIndexStore? sceneStore;
    private readonly FavoriteStore? favoriteStore;
    private readonly IClipExportService? clipExporter;
    private readonly IThumbnailService? thumbnailService;
    private readonly ICacheCatalog? cacheManager;
    private readonly ITextProvider? text;
    private readonly IClipDestinationPicker? clipDestinationPicker;
    private readonly Func<string, long>? availableSpace;
    private readonly Func<PlayerFeatureFactories>? featureFactory;
    private readonly PlayerLoadMetrics? metrics;
    private WorkspaceState state = WorkspaceState.Empty;
    private bool isPrivate;
    private string globalCacheStatus = "";

    public WorkspaceViewModel(Func<IPlayerBackend> backendFactory, IAudioSeparationService? audioSeparation = null,
        IVisionDetector? visionDetector = null, ISceneAnalysisService? sceneAnalysis = null, SceneIndexStore? sceneStore = null,
        FavoriteStore? favoriteStore = null, IClipExportService? clipExporter = null, IThumbnailService? thumbnailService = null,
        ICacheCatalog? cacheManager = null, SettingsViewModel? settings = null, ITextProvider? text = null,
        IClipDestinationPicker? clipDestinationPicker = null, Func<string, long>? availableSpace = null,
        Func<PlayerFeatureFactories>? featureFactory = null, PlayerLoadMetrics? metrics = null)
    {
        this.backendFactory = backendFactory;
        this.audioSeparation = audioSeparation;
        this.visionDetector = visionDetector;
        this.sceneAnalysis = sceneAnalysis;
        this.sceneStore = sceneStore;
        this.favoriteStore = favoriteStore;
        this.clipExporter = clipExporter;
        this.thumbnailService = thumbnailService;
        this.cacheManager = cacheManager;
        this.text = text;
        this.clipDestinationPicker = clipDestinationPicker;
        this.availableSpace = availableSpace;
        this.featureFactory = featureFactory;
        this.metrics = metrics;
        Settings = settings;
        Relocalize();
    }

    public ObservableCollection<PlayerPanelViewModel> Panels { get; } = [];
    public SettingsViewModel? Settings { get; }
    public LayoutMode Layout => state.Layout;
    public int Rows => Layout == LayoutMode.SplitVertical ? 2 : 1;
    public int Columns => Layout == LayoutMode.SplitHorizontal ? 2 : 1;
    public bool IsPrivate { get => isPrivate; private set => SetProperty(ref isPrivate, value); }
    public string GlobalCacheStatus { get => globalCacheStatus; private set => SetProperty(ref globalCacheStatus, value); }

    public void RefreshGlobalCache()
    {
        var bytes = cacheManager?.ScanGlobal().TotalBytes ?? 0;
        GlobalCacheStatus = $"{T("Cache", "Caché")} {(bytes < 1024 * 1024 ? $"{bytes / 1024d:0.#} KB" : $"{bytes / 1024d / 1024:0.#} MB")}";
    }

    public CacheReport GetGlobalCacheReport() => cacheManager?.ScanGlobal() ?? new([]);

    public CacheDeleteResult DeleteGlobalCache(CacheCategory category)
    {
        var result = cacheManager?.DeleteGlobal(category) ?? new(0, 0, []);
        RefreshGlobalCache();
        return result;
    }

    public async Task ActivatePrivacyAsync()
    {
        IsPrivate = true;
        foreach (var panel in Panels) await panel.ActivatePrivacyAsync();
    }

    public void RevealNames()
    {
        IsPrivate = false;
        foreach (var panel in Panels) panel.RevealName();
    }

    public Task OpenAsync(string path)
    {
        Add(path);
        return Task.CompletedTask;
    }

    public Task<DropResult> DropAsync(string path)
    {
        if (Panels.Count >= 2) return Task.FromResult(DropResult.ReplaceChoiceRequired);
        Add(path);
        return Task.FromResult(DropResult.Added);
    }

    public void ToggleLayout()
    {
        state = state.ToggleLayout();
        NotifyLayout();
    }

    public async Task ReplaceAsync(int index, string path)
    {
        if ((uint)index >= Panels.Count) throw new ArgumentOutOfRangeException(nameof(index));
        var old = Panels[index];
        old.CloseRequested -= Panel_CloseRequested;
        await old.DisposeAsync();
        state = state.Replace(state.Slots[index].Id, path);
        Panels[index] = CreatePanel(path);
    }

    public async Task CloseAsync(PlayerPanelViewModel panel)
    {
        var index = Panels.IndexOf(panel);
        if (index < 0) return;
        panel.CloseRequested -= Panel_CloseRequested;
        await panel.DisposeAsync();
        state = state.Remove(state.Slots[index].Id);
        Panels.RemoveAt(index);
        NotifyLayout();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var panel in Panels.ToArray())
        {
            panel.CloseRequested -= Panel_CloseRequested;
            await panel.DisposeAsync();
        }
        Panels.Clear();
        state = WorkspaceState.Empty;
        if (visionDetector is not null) await visionDetector.DisposeAsync();
    }

    private void Add(string path)
    {
        state = state.Add(path);
        Panels.Add(CreatePanel(path));
        NotifyLayout();
    }

    private PlayerPanelViewModel CreatePanel(string path)
    {
        var panel = new PlayerPanelViewModel(backendFactory(), path, audioSeparation, visionDetector, sceneAnalysis, sceneStore,
            favoriteStore, clipExporter, thumbnailService, cacheManager, text, clipDestinationPicker, availableSpace, Settings,
            features: featureFactory?.Invoke(), metrics: metrics);
        panel.CloseRequested += Panel_CloseRequested;
        return panel;
    }

    private async void Panel_CloseRequested(object? sender, EventArgs e)
    {
        if (sender is PlayerPanelViewModel panel) await CloseAsync(panel);
    }

    private void NotifyLayout()
    {
        OnPropertyChanged(nameof(Layout));
        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(Columns));
    }

    public void Relocalize()
    {
        GlobalCacheStatus = T("Cache", "Caché");
        foreach (var panel in Panels) panel.Relocalize();
    }

    private string T(string key, string fallback, params object[] args) => text?.Text(key, args) ?? string.Format(fallback, args);
}
