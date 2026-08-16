using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;
using PUPlayer.App.Playback;
using PUPlayer.App.AudioProcessing;
using PUPlayer.Core.Audio;
using PUPlayer.Core.Playback;
using PUPlayer.Core.Zoom;
using PUPlayer.App.Tracking;
using PUPlayer.Core.Scenes;
using System.Collections.ObjectModel;
using PUPlayer.Core.Favorites;
using PUPlayer.Core.MediaTools;
using PUPlayer.App.MediaTools;
using PUPlayer.Core.Cache;
using PUPlayer.App.Personalization;
using PUPlayer.App.Features;
using System.Windows;

namespace PUPlayer.App.ViewModels;

public sealed class PlayerPanelViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IPlayerBackend backend;
    private readonly ZoomInteraction zoom = new();
    private readonly CancellationTokenSource lifetime = new();
    private readonly SemaphoreSlim loadGate = new(1, 1);
    private readonly SynchronizationContext? context = SynchronizationContext.Current;
    private readonly PlayerFeatureFactories features;
    private readonly Lazy<VisionCoordinator?> vision;
    private CancellationTokenSource? audioProcessing;
    private Task? observer;
    private double positionSeconds;
    private double durationSeconds;
    private bool isPaused = true;
    private double volumePercent = 100;
    private double speed = 1;
    private string? error;
    private AudioPreset selectedAudioPreset = AudioPreset.Natural;
    private double lowCutHz;
    private double voiceGainDb;
    private double presenceGainDb;
    private double denoise;
    private bool compression;
    private bool isAudioProcessing;
    private bool isAiAudioActive;
    private string audioProcessingStatus = "";
    private bool disposed;
    private bool isTracking;
    private bool isSubjectSelectionRequired;
    private string trackingStatus = "";
    private bool isPrivate;
    private PlaybackLoop playbackLoop;
    private bool loopSeeking;
    private readonly SceneIndexStore? sceneStore;
    private readonly IFavoriteStore? favoriteStore;
    private CancellationTokenSource? sceneProcessing;
    private double sceneSensitivity = .7;
    private bool isSceneProcessing;
    private string sceneStatus = "";
    private readonly ICacheCatalog? cacheManager;
    private readonly ITextProvider? text;
    private readonly IClipDestinationPicker? clipDestinationPicker;
    private readonly Func<string, long> availableSpace;
    private readonly SettingsViewModel? settings;
    private CancellationTokenSource? clipProcessing;
    private double? clipStart;
    private double? clipEnd;
    private ClipExportMode clipMode;
    private bool isExporting;
    private double exportProgress;
    private string exportStatus = "";
    private VideoTransform geometry = new();
    private bool isCropEditing;
    private string cacheStatus = "";
    private readonly TaskCompletionSource playable = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Func<TimeSpan, CancellationToken, Task> delay;
    private bool isLoading;
    private bool hasPlayableFrame;
    private string loadStatus = "";
    private int favoritesLoaded;
    private readonly PlayerLoadMetrics? metrics;

    public PlayerPanelViewModel(IPlayerBackend backend, string? mediaPath = null, IAudioSeparationService? audioSeparation = null,
        IVisionDetector? visionDetector = null, ISceneAnalysisService? sceneAnalysis = null, SceneIndexStore? sceneStore = null,
        IFavoriteStore? favoriteStore = null, IClipExportService? clipExporter = null, IThumbnailService? thumbnailService = null,
        ICacheCatalog? cacheManager = null, ITextProvider? text = null, IClipDestinationPicker? clipDestinationPicker = null,
        Func<string, long>? availableSpace = null, SettingsViewModel? settings = null,
        Func<TimeSpan, CancellationToken, Task>? delay = null, PlayerFeatureFactories? features = null,
        PlayerLoadMetrics? metrics = null)
    {
        this.backend = backend;
        this.features = features ?? new(() => audioSeparation, () => visionDetector, () => sceneAnalysis, () => clipExporter, () => thumbnailService);
        vision = new(CreateVision, LazyThreadSafetyMode.ExecutionAndPublication);
        this.sceneStore = sceneStore;
        this.favoriteStore = favoriteStore;
        this.cacheManager = cacheManager;
        this.text = text;
        this.clipDestinationPicker = clipDestinationPicker;
        this.availableSpace = availableSpace ?? (path => new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))!).AvailableFreeSpace);
        this.settings = settings;
        this.delay = delay ?? Task.Delay;
        this.metrics = metrics;
        MediaPath = mediaPath;
        Relocalize();
        IsLoading = mediaPath is not null;
        LoadStatus = T("Loading", "Cargando…");
    }

    public event EventHandler? CloseRequested;
    public bool AiFeaturesAvailable => BuildCapabilities.AiAvailable;
    public Visibility AiFeaturesVisibility => AiFeaturesAvailable ? Visibility.Visible : Visibility.Collapsed;
    public string? MediaPath { get; }
    public string DisplayName => IsPrivate ? T("PrivateVideo", "Video privado") : Path.GetFileName(MediaPath) is { Length: > 0 } name ? name : T("Video", "Video");
    public string TimeLabel => $"{FormatTime(PositionSeconds)} / {FormatTime(DurationSeconds)}";
    public double ZoomScale => zoom.State.Scale;
    public double PositionSeconds { get => positionSeconds; private set => SetProperty(ref positionSeconds, value); }
    public double DurationSeconds { get => durationSeconds; private set => SetProperty(ref durationSeconds, value); }
    public bool IsPaused { get => isPaused; private set => SetProperty(ref isPaused, value); }
    public double VolumePercent { get => volumePercent; private set => SetProperty(ref volumePercent, value); }
    public double Speed { get => speed; private set => SetProperty(ref speed, value); }
    public string? Error { get => error; private set => SetProperty(ref error, value); }
    public AudioPreset SelectedAudioPreset { get => selectedAudioPreset; private set => SetProperty(ref selectedAudioPreset, value); }
    public double LowCutHz { get => lowCutHz; set => SetProperty(ref lowCutHz, value); }
    public double VoiceGainDb { get => voiceGainDb; set => SetProperty(ref voiceGainDb, value); }
    public double PresenceGainDb { get => presenceGainDb; set => SetProperty(ref presenceGainDb, value); }
    public double Denoise { get => denoise; set => SetProperty(ref denoise, value); }
    public bool Compression { get => compression; set => SetProperty(ref compression, value); }
    public bool IsAudioProcessing { get => isAudioProcessing; private set => SetProperty(ref isAudioProcessing, value); }
    public bool IsAiAudioActive { get => isAiAudioActive; private set => SetProperty(ref isAiAudioActive, value); }
    public string AudioProcessingStatus { get => audioProcessingStatus; private set => SetProperty(ref audioProcessingStatus, value); }
    public bool IsTracking { get => isTracking; private set => SetProperty(ref isTracking, value); }
    public bool IsSubjectSelectionRequired { get => isSubjectSelectionRequired; private set => SetProperty(ref isSubjectSelectionRequired, value); }
    public string TrackingStatus { get => trackingStatus; private set => SetProperty(ref trackingStatus, value); }
    public bool IsPrivate { get => isPrivate; private set { if (SetProperty(ref isPrivate, value)) OnPropertyChanged(nameof(DisplayName)); } }
    public double? LoopStart => playbackLoop.Start;
    public double? LoopEnd => playbackLoop.End;
    public bool IsLoopActive => playbackLoop.IsActive;
    public string LoopLabel => playbackLoop.IsActive ? $"A {FormatTime(playbackLoop.Start!.Value)}  ·  B {FormatTime(playbackLoop.End!.Value)}" : T("LoopUndefined", "Bucle sin definir");
    public ObservableCollection<SceneMarker> SceneMarkers { get; } = [];
    public double SceneSensitivity { get => sceneSensitivity; set => SetProperty(ref sceneSensitivity, Math.Clamp(value, 0, 1)); }
    public bool IsSceneProcessing { get => isSceneProcessing; private set => SetProperty(ref isSceneProcessing, value); }
    public string SceneStatus { get => sceneStatus; private set => SetProperty(ref sceneStatus, value); }
    public double? ClipStart => clipStart;
    public double? ClipEnd => clipEnd;
    public string ClipLabel => clipStart is { } a && clipEnd is { } b ? $"{FormatTime(a)} — {FormatTime(b)}  ·  {Math.Abs(b - a):0.0} s" : T("DefineAB", "Define los puntos A y B");
    public ClipExportMode ClipMode { get => clipMode; set { if (SetProperty(ref clipMode, value)) OnPropertyChanged(nameof(ClipModeLabel)); } }
    public string ClipModeLabel => ClipMode == ClipExportMode.Original ? T("ModeQuick", "Modo: original rápido") : T("ModeView", "Modo: aplicar vista");
    public bool IsExporting { get => isExporting; private set => SetProperty(ref isExporting, value); }
    public double ExportProgress { get => exportProgress; private set => SetProperty(ref exportProgress, value); }
    public string ExportStatus { get => exportStatus; private set => SetProperty(ref exportStatus, value); }
    public VideoTransform Geometry { get => geometry; private set => SetProperty(ref geometry, value); }
    public bool IsCropEditing { get => isCropEditing; set => SetProperty(ref isCropEditing, value); }
    public string CacheStatus { get => cacheStatus; private set => SetProperty(ref cacheStatus, value); }
    public bool IsLoading { get => isLoading; private set => SetProperty(ref isLoading, value); }
    public bool HasPlayableFrame { get => hasPlayableFrame; private set => SetProperty(ref hasPlayableFrame, value); }
    public string LoadStatus { get => loadStatus; private set => SetProperty(ref loadStatus, value); }
    public Task WaitForPlayableFrameAsync() => playable.Task;

    public void MarkClipStart() => SetClipMarks(PositionSeconds, clipEnd);
    public void MarkClipEnd() => SetClipMarks(clipStart, PositionSeconds);
    public void ClearClipMarks() => SetClipMarks(null, null);
    public void SetClipMarks(double? start, double? end)
    {
        clipStart = start; clipEnd = end;
        OnPropertyChanged(nameof(ClipStart)); OnPropertyChanged(nameof(ClipEnd)); OnPropertyChanged(nameof(ClipLabel));
    }

    public async Task ExportClipAsync()
    {
        if (IsExporting || MediaPath is null || clipStart is null || clipEnd is null) return;
        var clipExporter = features.Clips;
        if (clipExporter is null) return;
        ClipSelection selection;
        try { selection = ClipSelection.FromMarks(clipStart.Value, clipEnd.Value, DurationSeconds > 0 ? DurationSeconds : double.MaxValue); }
        catch (ArgumentException e) { ExportStatus = e.Message; return; }
        var suggested = ClipOutputNamer.Next(MediaPath);
        if (settings?.Value.LastExportDirectory is { } directory) suggested = Path.Combine(directory, Path.GetFileName(suggested));
        var output = clipDestinationPicker?.Pick(suggested) ?? (clipDestinationPicker is null ? suggested : null);
        if (output is null) { ExportStatus = T("ExportCanceled", "Exportación cancelada"); return; }
        var estimate = ClipSizeEstimator.Estimate(new FileInfo(MediaPath).Length, Math.Max(DurationSeconds, selection.End), selection.Duration,
            ClipMode == ClipExportMode.Original ? ClipEstimateMode.Original : ClipEstimateMode.CurrentView);
        var free = availableSpace(output);
        if (free < estimate) { ExportStatus = T("InsufficientSpace", "Espacio insuficiente: se requieren {0} y hay {1} disponibles.", FormatBytes(estimate), FormatBytes(free)); return; }
        settings?.SetLastExportDirectory(Path.GetDirectoryName(output));
        clipProcessing = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        IsExporting = true; ExportProgress = 0; ExportStatus = T("PreparingClip", "Preparando clip…");
        try
        {
            var progress = new InlineProgress<ClipExportProgress>(x => { ExportProgress = x.Fraction * 100; ExportStatus = x.Message; });
            await clipExporter.ExportAsync(new(MediaPath, selection, output, ClipMode, Geometry), progress, clipProcessing.Token);
            ExportStatus = T("ClipSaved", "Clip guardado"); ExportProgress = 100;
        }
        catch (OperationCanceledException) { ExportStatus = T("ExportCanceled", "Exportación cancelada"); }
        catch (Exception e) { Error = e.Message; ExportStatus = T("ExportFailed", "No se pudo exportar"); }
        finally { clipProcessing.Dispose(); clipProcessing = null; IsExporting = false; }
    }

    public void CancelClipExport() => clipProcessing?.Cancel();

    public Task RotateRightAsync() => SetGeometryAsync(new((Geometry.Rotation + 90) % 360, Geometry.MirrorX, Geometry.MirrorY, Geometry.Crop));
    public Task RotateLeftAsync() => SetGeometryAsync(new((Geometry.Rotation + 270) % 360, Geometry.MirrorX, Geometry.MirrorY, Geometry.Crop));
    public Task ToggleMirrorXAsync() => SetGeometryAsync(new(Geometry.Rotation, !Geometry.MirrorX, Geometry.MirrorY, Geometry.Crop));
    public Task ToggleMirrorYAsync() => SetGeometryAsync(new(Geometry.Rotation, Geometry.MirrorX, !Geometry.MirrorY, Geometry.Crop));
    public Task SetCropAsync(CropRect crop) => SetGeometryAsync(new(Geometry.Rotation, Geometry.MirrorX, Geometry.MirrorY, crop));
    public Task ResetGeometryAsync() => SetGeometryAsync(new());

    private async Task SetGeometryAsync(VideoTransform value)
    {
        Geometry = value;
        OnPropertyChanged(nameof(Geometry));
        await ApplyGeometryAsync(value, lifetime.Token);
    }

    public Task<string?> GetThumbnailAsync(double seconds, CancellationToken cancellationToken = default) =>
        MediaPath is null || DurationSeconds <= 0 ? Task.FromResult<string?>(null) : GetThumbnailCoreAsync(seconds, cancellationToken);

    private async Task<string?> GetThumbnailCoreAsync(double seconds, CancellationToken cancellationToken) =>
        features.Thumbnails is { } service ? await service.GetAsync(MediaPath!, DurationSeconds, seconds, cancellationToken) : null;

    public void RefreshCache()
    {
        if (cacheManager is null || MediaPath is null) return;
        var report = cacheManager.ScanVideo(MediaPath);
        CacheStatus = T("CacheSummary", "Audio {0} · Miniaturas {1} · Análisis {2}", FormatBytes(report.Bytes(CacheCategory.Audio)), FormatBytes(report.Bytes(CacheCategory.Thumbnails)), FormatBytes(report.Bytes(CacheCategory.Analysis)));
    }

    public CacheReport GetCacheReport() => cacheManager is null || MediaPath is null ? new([]) : cacheManager.ScanVideo(MediaPath);

    public CacheDeleteResult DeleteCache(CacheCategory category)
    {
        if (cacheManager is null || MediaPath is null) return new(0, 0, []);
        var result = cacheManager.DeleteVideo(MediaPath, category);
        RefreshCache();
        return result;
    }

    public void SetLoopStart() { playbackLoop = playbackLoop.WithStart(PositionSeconds); NotifyLoop(); }
    public void SetLoopEnd() { playbackLoop = playbackLoop.WithEnd(PositionSeconds); NotifyLoop(); }
    public void ClearLoop() { playbackLoop = default; NotifyLoop(); }

    public async Task AnalyzeScenesAsync()
    {
        if (IsSceneProcessing || MediaPath is null) return;
        var sceneAnalysis = features.Scenes;
        if (sceneAnalysis is null) return;
        sceneProcessing = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        IsSceneProcessing = true; SceneStatus = T("AnalyzingScenes", "Analizando escenas…");
        try
        {
            var index = await sceneAnalysis.AnalyzeAsync(MediaPath, SceneSensitivity, sceneProcessing.Token);
            SceneMarkers.Clear();
            foreach (var marker in index.Markers) SceneMarkers.Add(marker with { Label = MarkerLabel(marker.Kind) });
            SceneStatus = T("MarkersReady", "{0} marcadores listos", SceneMarkers.Count);
        }
        catch (OperationCanceledException) { SceneStatus = T("AnalysisCanceled", "Análisis cancelado"); }
        catch (Exception exception) { Error = exception.Message; SceneStatus = T("AnalysisFailed", "No se pudieron analizar las escenas"); }
        finally { sceneProcessing.Dispose(); sceneProcessing = null; IsSceneProcessing = false; }
    }

    public void CancelSceneAnalysis() => sceneProcessing?.Cancel();

    public void AddFavorite()
    {
        if (MediaPath is null) return;
        if (SceneMarkers.Any(x => x.Kind == SceneMarkerKind.Favorite && Math.Abs(x.Seconds - PositionSeconds) < .1)) return;
        SceneMarkers.Add(new(PositionSeconds, SceneMarkerKind.Favorite, T("Favorite", "Favorito")));
        SaveFavorites();
        SceneStatus = T("MarkersReady", "{0} marcadores listos", SceneMarkers.Count);
    }

    public void RemoveFavorite(SceneMarker marker)
    {
        if (marker.Kind != SceneMarkerKind.Favorite || !SceneMarkers.Remove(marker)) return;
        SaveFavorites();
    }

    private async Task LoadFavoritesAsync()
    {
        if (MediaPath is null || favoriteStore is null || Interlocked.Exchange(ref favoritesLoaded, 1) != 0) return;
        var seconds = (await Task.Run(() => favoriteStore.Load(MediaPath), lifetime.Token)).Seconds;
        void ApplyFavorites()
        {
            foreach (var second in seconds)
                if (!SceneMarkers.Any(x => x.Kind == SceneMarkerKind.Favorite && Math.Abs(x.Seconds - second) < .1))
                    SceneMarkers.Add(new(second, SceneMarkerKind.Favorite, T("Favorite", "Favorito")));
        }
        if (context is null) ApplyFavorites();
        else
        {
            var applied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            context.Post(_ => { ApplyFavorites(); applied.TrySetResult(); }, null);
            await applied.Task;
        }
    }

    private void SaveFavorites()
    {
        if (MediaPath is not null)
            favoriteStore?.Save(MediaPath, SceneMarkers.Where(x => x.Kind == SceneMarkerKind.Favorite).Select(x => x.Seconds));
    }

    public Task SeekMarkerAsync(SceneMarker marker) => SeekAsync(marker.Seconds);

    public async Task ActivatePrivacyAsync(CancellationToken cancellationToken = default)
    {
        IsPrivate = true;
        await SetVolumeAsync(0, cancellationToken);
    }

    public void RevealName() => IsPrivate = false;

    public async Task LoadAsync(nint windowHandle, CancellationToken cancellationToken = default)
    {
        if (MediaPath is null) return;
        await loadGate.WaitAsync(cancellationToken);
        try
        {
            if (disposed) return;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, cancellationToken);
            IsLoading = true;
            LoadStatus = T("Loading", "Cargando…");
            observer ??= ObserveAsync();
            _ = ReportSlowAsync(linked.Token);
            await backend.LoadAsync(MediaPath, windowHandle, linked.Token);
            metrics?.MarkWorkerReady();
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        catch (Exception exception)
        {
            Error = exception.Message;
            LoadStatus = T("LoadFailed", "No se pudo cargar");
            IsLoading = false;
        }
        finally { loadGate.Release(); }
    }

    private async Task ReportSlowAsync(CancellationToken cancellationToken)
    {
        try
        {
            await delay(TimeSpan.FromSeconds(10), cancellationToken);
            if (IsLoading) LoadStatus = T("StillLoading", "Está tardando…");
        }
        catch (OperationCanceledException) { }
    }

    public Task PlayPauseAsync(CancellationToken cancellationToken = default) =>
        SetPausedAsync(!IsPaused, cancellationToken);

    public Task SeekAsync(double seconds, CancellationToken cancellationToken = default) =>
        backend.SeekAsync(Math.Max(0, seconds), cancellationToken);

    public Task SkipAsync(double seconds, CancellationToken cancellationToken = default) =>
        SeekAsync(PositionSeconds + seconds, cancellationToken);

    public async Task SetPausedAsync(bool value, CancellationToken cancellationToken = default)
    {
        IsPaused = value;
        await backend.SetPausedAsync(value, cancellationToken);
    }

    public async Task SetVolumeAsync(double percent, CancellationToken cancellationToken = default)
    {
        VolumePercent = Math.Clamp(percent, 0, 200);
        await backend.SetVolumeAsync(VolumePercent, cancellationToken);
    }

    public async Task SetSpeedAsync(double value, CancellationToken cancellationToken = default)
    {
        Speed = Math.Clamp(value, .25, 4);
        await backend.SetSpeedAsync(Speed, cancellationToken);
    }

    public Task SetTransformAsync(MpvTransform value, CancellationToken cancellationToken = default) =>
        backend.SetTransformAsync(value, cancellationToken);

    public Task ApplyGeometryAsync(VideoTransform value, CancellationToken cancellationToken = default) =>
        backend.SetGeometryAsync(value, cancellationToken);

    public async Task ZoomWheelAsync(int delta, NormalizedPoint cursor, CancellationToken cancellationToken = default)
    {
        await StopTrackingAsync();
        zoom.Wheel(delta, cursor);
        OnPropertyChanged(nameof(ZoomScale));
        await SetTransformAsync(zoom.State.ToMpv(), cancellationToken);
    }

    public async Task PanAsync(double dx, double dy, CancellationToken cancellationToken = default)
    {
        await StopTrackingAsync();
        zoom.Drag(dx, dy);
        await SetTransformAsync(zoom.State.ToMpv(), cancellationToken);
    }

    public async Task ResetZoomAsync(CancellationToken cancellationToken = default)
    {
        zoom.Reset();
        OnPropertyChanged(nameof(ZoomScale));
        await SetTransformAsync(zoom.State.ToMpv(), cancellationToken);
    }

    public async Task StartTrackingAsync()
    {
        var coordinator = vision.Value;
        if (coordinator is null) { TrackingStatus = "Seguimiento no instalado"; return; }
        IsTracking = true;
        await coordinator.StartAsync();
    }

    public async Task StopTrackingAsync()
    {
        if (vision.IsValueCreated && vision.Value is { } coordinator) await coordinator.StopAsync();
        IsTracking = false;
        IsSubjectSelectionRequired = false;
        TrackingStatus = T("TrackingOff", "Seguimiento desactivado");
    }

    public Task SelectSubjectAsync(NormalizedPoint point, CancellationToken cancellationToken = default) =>
        vision.IsValueCreated && vision.Value is { } coordinator ? coordinator.SelectAsync(point, cancellationToken) : Task.CompletedTask;

    public async Task ApplyAudioPresetAsync(AudioPreset preset, CancellationToken cancellationToken = default)
    {
        SelectedAudioPreset = preset;
        SetAudioSettings(AudioSettings.FromPreset(preset));
        await ApplyManualAudioAsync(cancellationToken);
    }

    public Task ApplyManualAudioAsync(CancellationToken cancellationToken = default) =>
        backend.SetAudioFilterAsync(MpvAudioFilterBuilder.Build(CurrentAudioSettings()), cancellationToken);

    public Task LoadExternalAudioAsync(string path, CancellationToken cancellationToken = default) =>
        backend.LoadExternalAudioAsync(path, cancellationToken);

    public Task EnhanceVoiceAsync() => EnhanceAudioAsync(false);
    public Task EnhanceDetailAsync() => EnhanceAudioAsync(true);

    private async Task EnhanceAudioAsync(bool detail)
    {
        if (IsAudioProcessing || MediaPath is null) return;
        var audioSeparation = features.Audio;
        if (audioSeparation is null) return;
        audioProcessing = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        IsAudioProcessing = true;
        var progress = new InlineProgress<AudioProcessingProgress>(value => AudioProcessingStatus = value.Message);
        try
        {
            var path = detail
                ? await audioSeparation.GetOrCreateDetailCacheAsync(MediaPath, progress, audioProcessing.Token)
                : await audioSeparation.GetOrCreateVoiceCacheAsync(MediaPath, progress, audioProcessing.Token);
            await backend.LoadExternalAudioAsync(path, audioProcessing.Token);
            IsAiAudioActive = true;
            Error = null;
            AudioProcessingStatus = detail ? T("DetailActive", "Detalle íntimo activo") : T("VoiceActive", "Voz mejorada activa");
        }
        catch (OperationCanceledException) { AudioProcessingStatus = T("ProcessingCanceled", "Procesamiento cancelado"); }
        catch (Exception exception) { Error = exception.Message; AudioProcessingStatus = T("VoiceFailed", "No se pudo mejorar la voz"); }
        finally
        {
            audioProcessing.Dispose();
            audioProcessing = null;
            IsAudioProcessing = false;
        }
    }

    public void CancelVoiceEnhancement() => audioProcessing?.Cancel();

    public async Task UseOriginalAudioAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAiAudioActive) return;
        await backend.UseOriginalAudioAsync(cancellationToken);
        IsAiAudioActive = false;
        Error = null;
        AudioProcessingStatus = T("OriginalAudio", "Audio original");
    }

    public void RequestClose() => CloseRequested?.Invoke(this, EventArgs.Empty);

    public async ValueTask DisposeAsync()
    {
        audioProcessing?.Cancel();
        sceneProcessing?.Cancel();
        clipProcessing?.Cancel();
        lifetime.Cancel();
        await loadGate.WaitAsync();
        try
        {
            disposed = true;
            if (vision.IsValueCreated && vision.Value is { } coordinator) { coordinator.Updated -= Vision_Updated; await coordinator.DisposeAsync(); }
            await backend.DisposeAsync();
        }
        finally { loadGate.Release(); }
        if (observer is not null) try { await observer; } catch (OperationCanceledException) { }
        lifetime.Dispose();
    }

    private async Task ObserveAsync()
    {
        try
        {
            await foreach (var snapshot in backend.Snapshots(lifetime.Token))
            {
                lifetime.Token.ThrowIfCancellationRequested();
                if (context is null) Apply(snapshot); else context.Post(_ => Apply(snapshot), null);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            if (context is null) Error = exception.Message; else context.Post(_ => Error = exception.Message, null);
        }
    }

    private void Apply(PlayerSnapshot snapshot)
    {
        PositionSeconds = snapshot.PositionSeconds;
        DurationSeconds = snapshot.DurationSeconds;
        IsPaused = snapshot.Paused;
        Speed = snapshot.Speed;
        VolumePercent = snapshot.VolumePercent;
        OnPropertyChanged(nameof(TimeLabel));
        if (!HasPlayableFrame && snapshot.DurationSeconds > 0)
        {
            HasPlayableFrame = true;
            IsLoading = false;
            metrics?.MarkFirstPlayableFrame();
            _ = CompletePlayableAsync();
        }
        if (!loopSeeking && playbackLoop.SeekTarget(PositionSeconds) is { } target) _ = RewindLoopAsync(target);
    }

    private VisionCoordinator? CreateVision()
    {
        if (features.Vision is not { } detector) return null;
        var coordinator = new VisionCoordinator(backend, detector, text);
        coordinator.Updated += Vision_Updated;
        return coordinator;
    }

    private async Task CompletePlayableAsync()
    {
        try { await LoadFavoritesAsync(); }
        catch (OperationCanceledException) { }
        catch { }
        finally { playable.TrySetResult(); }
    }

    private async Task RewindLoopAsync(double target)
    {
        loopSeeking = true;
        try { await SeekAsync(target, lifetime.Token); }
        catch (OperationCanceledException) { }
        finally { loopSeeking = false; }
    }

    private void NotifyLoop()
    {
        OnPropertyChanged(nameof(LoopStart));
        OnPropertyChanged(nameof(LoopEnd));
        OnPropertyChanged(nameof(IsLoopActive));
        OnPropertyChanged(nameof(LoopLabel));
    }

    private void Vision_Updated(VisionUpdate update)
    {
        void ApplyUpdate()
        {
            TrackingStatus = update.Status;
            IsSubjectSelectionRequired = update.NeedsSelection;
            if (update.Transform is not null) OnPropertyChanged(nameof(ZoomScale));
        }
        if (context is null) ApplyUpdate(); else context.Post(_ => ApplyUpdate(), null);
    }

    public void Relocalize()
    {
        if (!IsAudioProcessing) AudioProcessingStatus = IsAiAudioActive ? AudioProcessingStatus : T("OriginalAudio", "Audio original");
        if (!IsTracking) TrackingStatus = T("TrackingOff", "Seguimiento desactivado");
        if (!IsSceneProcessing && SceneMarkers.Count == 0) SceneStatus = T("ScenesUnanalyzed", "Escenas sin analizar");
        if (!IsExporting) ExportStatus = clipStart is null || clipEnd is null ? T("DefineAB", "Define los puntos A y B") : ExportStatus;
        if (CacheStatus.Length == 0 || CacheStatus == "Caché sin calcular" || CacheStatus == "Cache not calculated") CacheStatus = T("CacheUncalculated", "Caché sin calcular");
        if (IsLoading) LoadStatus = T(LoadStatus.Contains("tard", StringComparison.OrdinalIgnoreCase) || LoadStatus.Contains("Still", StringComparison.OrdinalIgnoreCase) ? "StillLoading" : "Loading", "Cargando…");
        for (var index = 0; index < SceneMarkers.Count; index++) SceneMarkers[index] = SceneMarkers[index] with { Label = MarkerLabel(SceneMarkers[index].Kind) };
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(LoopLabel));
        OnPropertyChanged(nameof(ClipLabel));
        OnPropertyChanged(nameof(ClipModeLabel));
    }

    private string MarkerLabel(SceneMarkerKind kind) => kind switch
    {
        SceneMarkerKind.Voice => T("Voice", "Voz"),
        SceneMarkerKind.Detail => T("Detail", "Detalle"),
        SceneMarkerKind.HighActivity => T("HighActivity", "Actividad alta"),
        _ => T("Favorite", "Favorito")
    };

    private string T(string key, string fallback, params object[] args) => text?.Text(key, args) ?? string.Format(fallback, args);

    private static string FormatTime(double seconds) => TimeSpan.FromSeconds(Math.Max(0, seconds)).ToString(seconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss");
    private static string FormatBytes(long bytes) => bytes < 1024 * 1024 ? $"{bytes / 1024d:0.#} KB" : $"{bytes / 1024d / 1024:0.#} MB";

    private AudioSettings CurrentAudioSettings() => new(LowCutHz, VoiceGainDb, PresenceGainDb, Denoise, Compression);

    private void SetAudioSettings(AudioSettings value)
    {
        LowCutHz = value.LowCutHz;
        VoiceGainDb = value.VoiceGainDb;
        PresenceGainDb = value.PresenceGainDb;
        Denoise = value.Denoise;
        Compression = value.Compression;
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
