using PUPlayer.App.ViewModels;
using PUPlayer.App.AudioProcessing;
using PUPlayer.Core.Audio;
using PUPlayer.Core.Zoom;
using PUPlayer.IntegrationTests.Fakes;
using PUPlayer.App.Tracking;
using PUPlayer.Core.Playback;
using PUPlayer.Core.Tracking;
using PUPlayer.Core.Favorites;
using System.IO;
using PUPlayer.Core.MediaTools;
using PUPlayer.App.MediaTools;
using PUPlayer.Core.Cache;
using PUPlayer.App.Features;

namespace PUPlayer.IntegrationTests.Playback;

public sealed class PlayerPanelViewModelTests
{
    [Fact]
    public async Task Favorites_LoadAfterFirstPlayableSnapshot()
    {
        var root = Path.Combine(Path.GetTempPath(), "IgoonTube-panel-favorites-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var video = Path.Combine(root, "video.mp4");
            File.WriteAllText(video, "video");
            var store = new FavoriteStore();
            store.Save(video, [12.5]);

            var backend = new FakePlayerBackend();
            var panel = new PlayerPanelViewModel(backend, video, favoriteStore: store);

            Assert.Empty(panel.SceneMarkers);
            await panel.LoadAsync(1);
            backend.Publish(new(0, 60, false, 1, 100));
            await panel.WaitForPlayableFrameAsync().WaitAsync(TimeSpan.FromSeconds(1));

            var marker = Assert.Single(panel.SceneMarkers);
            Assert.Equal(12.5, marker.Seconds);
            Assert.Equal(PUPlayer.Core.Scenes.SceneMarkerKind.Favorite, marker.Kind);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task Load_RemainsLoadingUntilFirstPlayableSnapshot()
    {
        var backend = new FakePlayerBackend();
        var panel = new PlayerPanelViewModel(backend, "video.mp4");

        await panel.LoadAsync(1);
        Assert.True(panel.IsLoading);

        backend.Publish(new(0, 60, false, 1, 100));
        await panel.WaitForPlayableFrameAsync().WaitAsync(TimeSpan.FromSeconds(1));

        Assert.False(panel.IsLoading);
        Assert.True(panel.HasPlayableFrame);
    }

    [Fact]
    public async Task Load_ReportsSlowAfterConfiguredDelay()
    {
        var delayed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var panel = new PlayerPanelViewModel(new FakePlayerBackend(), "video.mp4",
            delay: (_, _) => delayed.Task);

        await panel.LoadAsync(1);
        delayed.SetResult();
        await Task.Yield();

        Assert.Contains("tardando", panel.LoadStatus, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoadMetrics_RecordOnlyFirstMilestone()
    {
        long timestamp = 0;
        var metrics = new PlayerLoadMetrics(() => ++timestamp);
        metrics.MarkWindowVisible();
        metrics.MarkWindowVisible();
        var backend = new FakePlayerBackend();
        var panel = new PlayerPanelViewModel(backend, "video.mp4", metrics: metrics);

        await panel.LoadAsync(1);
        backend.Publish(new(0, 60, false, 1, 100));
        await panel.WaitForPlayableFrameAsync();

        Assert.Equal(1, metrics.WindowVisible);
        Assert.Equal(2, metrics.WorkerReady);
        Assert.Equal(3, metrics.FirstPlayableFrame);
    }

    [Fact]
    public async Task OptionalFeatures_AreCreatedOnlyOnFirstUse()
    {
        var video = Path.GetTempFileName();
        var output = video + ".clip.mp4";
        try
        {
            var calls = new int[5];
            var backend = new FakePlayerBackend();
            var features = new PlayerFeatureFactories(
                () => { calls[0]++; return new FakeSeparation(video); },
                () => { calls[1]++; return new FakeVisionDetector([Body(.5)]); },
                () => { calls[2]++; return new FakeSceneAnalysis(video); },
                () => { calls[3]++; return new RecordingClipExporter(); },
                () => { calls[4]++; return new FakeThumbnailService(); });
            await using var panel = new PlayerPanelViewModel(backend, video,
                clipDestinationPicker: new FixedPicker(output), availableSpace: _ => long.MaxValue, features: features);

            await panel.LoadAsync(1);
            Assert.Equal([0, 0, 0, 0, 0], calls);
            backend.Publish(new(0, 60, false, 1, 100));
            await panel.WaitForPlayableFrameAsync();

            await panel.EnhanceVoiceAsync();
            await panel.StartTrackingAsync();
            await panel.AnalyzeScenesAsync();
            _ = await panel.GetThumbnailAsync(1);
            panel.SetClipMarks(1, 2);
            await panel.ExportClipAsync();

            Assert.Equal([1, 1, 1, 1, 1], calls);
        }
        finally
        {
            File.Delete(video);
            File.Delete(output);
        }
    }

    [Fact]
    public async Task Seek_ChangesOnlyItsBackend()
    {
        var left = new FakePlayerBackend();
        var right = new FakePlayerBackend();
        var a = new PlayerPanelViewModel(left);
        _ = new PlayerPanelViewModel(right);

        await a.SeekAsync(73);

        Assert.Equal(["seek:73"], left.Calls);
        Assert.Empty(right.Calls);
    }

    [Fact]
    public async Task Volume_IsClampedToTwoHundred()
    {
        var backend = new FakePlayerBackend();

        await new PlayerPanelViewModel(backend).SetVolumeAsync(250);

        Assert.Equal(["volume:200"], backend.Calls);
    }

    [Fact]
    public async Task Zoom_ChangesOnlyItsBackend()
    {
        var left = new FakePlayerBackend();
        var right = new FakePlayerBackend();
        var panel = new PlayerPanelViewModel(left);
        _ = new PlayerPanelViewModel(right);

        await panel.ZoomWheelAsync(120, new NormalizedPoint(.8, .3));

        Assert.Single(left.Calls, call => call.StartsWith("transform:", StringComparison.Ordinal));
        Assert.Empty(right.Calls);
    }

    [Fact]
    public async Task Geometry_ChangesOnlyItsBackend()
    {
        var left = new FakePlayerBackend();
        var right = new FakePlayerBackend();
        var panel = new PlayerPanelViewModel(left);
        _ = new PlayerPanelViewModel(right);

        await panel.ApplyGeometryAsync(new VideoTransform(90, true));

        Assert.Single(left.Calls, call => call.StartsWith("geometry:", StringComparison.Ordinal));
        Assert.Empty(right.Calls);
    }

    [Fact]
    public async Task GeometryControls_UpdateCurrentView()
    {
        var backend = new FakePlayerBackend();
        var panel = new PlayerPanelViewModel(backend);

        await panel.RotateRightAsync();
        await panel.ToggleMirrorXAsync();

        Assert.Equal(90, panel.Geometry.Rotation);
        Assert.True(panel.Geometry.MirrorX);
        Assert.Equal(2, backend.Calls.Count(x => x.StartsWith("geometry:", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ClipExport_UsesSelectedRangeAndUniqueOutput()
    {
        var root = Path.Combine(Path.GetTempPath(), "IgoonTube-panel-export-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var video = Path.Combine(root, "video.mp4");
            File.WriteAllText(video, "video");
            var service = new RecordingClipExporter();
            var panel = new PlayerPanelViewModel(new FakePlayerBackend(), video, clipExporter: service);
            panel.SetClipMarks(3, 17);

            await panel.ExportClipAsync();

            Assert.Equal(new ClipSelection(3, 17), service.Request!.Selection);
            Assert.EndsWith("video_clip_001.mp4", service.Request.Output);
            Assert.Equal("Clip guardado", panel.ExportStatus);
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public async Task ClipExport_CancelledDestination_DoesNotExport()
    {
        var video = Path.GetTempFileName();
        try
        {
            var service = new RecordingClipExporter();
            var panel = new PlayerPanelViewModel(new FakePlayerBackend(), video, clipExporter: service,
                clipDestinationPicker: new FixedPicker(null), availableSpace: _ => long.MaxValue);
            panel.SetClipMarks(1, 2);
            await panel.ExportClipAsync();
            Assert.Null(service.Request);
        }
        finally { File.Delete(video); }
    }

    [Fact]
    public async Task ClipExport_UsesChosenDestination()
    {
        var video = Path.GetTempFileName();
        var output = Path.Combine(Path.GetDirectoryName(video)!, "chosen.mp4");
        try
        {
            File.WriteAllBytes(video, new byte[1000]);
            var service = new RecordingClipExporter();
            var panel = new PlayerPanelViewModel(new FakePlayerBackend(), video, clipExporter: service,
                clipDestinationPicker: new FixedPicker(output), availableSpace: _ => long.MaxValue);
            panel.SetClipMarks(1, 2);
            await panel.ExportClipAsync();
            Assert.Equal(output, service.Request!.Output);
        }
        finally { File.Delete(video); }
    }

    [Fact]
    public async Task ClipExport_InsufficientSpace_DoesNotStartExporter()
    {
        var video = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(video, new byte[1_000_000]);
            var service = new RecordingClipExporter();
            var panel = new PlayerPanelViewModel(new FakePlayerBackend(), video, clipExporter: service,
                clipDestinationPicker: new FixedPicker(video + ".mp4"), availableSpace: _ => 1);
            panel.SetClipMarks(1, 2);
            await panel.ExportClipAsync();
            Assert.Null(service.Request);
            Assert.Contains("insuficiente", panel.ExportStatus, StringComparison.OrdinalIgnoreCase);
        }
        finally { File.Delete(video); }
    }

    private sealed class RecordingClipExporter : IClipExportService
    {
        public ClipExportRequest? Request { get; private set; }
        public Task<string> ExportAsync(ClipExportRequest request, IProgress<ClipExportProgress>? progress, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(request.Output);
        }
    }

    private sealed class FixedPicker(string? path) : IClipDestinationPicker
    {
        public string? Pick(string defaultPath) => path;
    }

    private sealed class FakeSceneAnalysis(string mediaPath) : ISceneAnalysisService
    {
        public Task<PUPlayer.Core.Scenes.SceneIndex> AnalyzeAsync(string _, double sensitivity, CancellationToken cancellationToken) =>
            Task.FromResult(PUPlayer.Core.Scenes.SceneIndex.For(mediaPath, sensitivity, []));
    }

    private sealed class FakeThumbnailService : IThumbnailService
    {
        public Task<string> GetAsync(string source, double duration, double seconds, CancellationToken cancellationToken) =>
            Task.FromResult(source + ".jpg");
    }

    [Fact]
    public async Task AudioPreset_ChangesOnlyItsBackend()
    {
        var left = new FakePlayerBackend();
        var right = new FakePlayerBackend();
        var panel = new PlayerPanelViewModel(left);
        _ = new PlayerPanelViewModel(right);

        await panel.ApplyAudioPresetAsync(AudioPreset.Voice);

        Assert.Single(left.Calls, call => call.StartsWith("filter:lavfi=", StringComparison.Ordinal));
        Assert.Empty(right.Calls);
    }

    [Fact]
    public async Task VoiceEnhancementLoadsOnlyItsCacheAndCanRestoreOriginalAudio()
    {
        var backend = new FakePlayerBackend();
        var panel = new PlayerPanelViewModel(backend, @"F:\video.mp4", new FakeSeparation(@"F:\voice.mka"));

        await panel.EnhanceVoiceAsync();
        await panel.UseOriginalAudioAsync();

        Assert.Equal([@"audio:F:\voice.mka", "audio:original"], backend.Calls);
        Assert.False(panel.IsAiAudioActive);
    }

    [Fact]
    public async Task VoiceEnhancementCanBeCanceled()
    {
        var service = new FakeSeparation();
        var panel = new PlayerPanelViewModel(new FakePlayerBackend(), @"F:\video.mp4", service);

        var processing = panel.EnhanceVoiceAsync();
        await service.Started.Task;
        panel.CancelVoiceEnhancement();
        await processing;

        Assert.False(panel.IsAudioProcessing);
        Assert.Equal("Procesamiento cancelado", panel.AudioProcessingStatus);
    }

    [Fact]
    public async Task DetailEnhancement_LoadsOnlyItsPanel()
    {
        var left = new FakePlayerBackend();
        var right = new FakePlayerBackend();
        var panel = new PlayerPanelViewModel(left, @"F:\video.mp4", new FakeSeparation(@"F:\detail.mka"));
        _ = new PlayerPanelViewModel(right);

        await panel.EnhanceDetailAsync();

        Assert.Equal([@"audio:F:\detail.mka"], left.Calls);
        Assert.Empty(right.Calls);
        Assert.Equal("Detalle íntimo activo", panel.AudioProcessingStatus);
    }

    [Fact]
    public async Task Tracking_ChangesOnlyItsPanel_AndManualZoomStopsIt()
    {
        var left = new FakePlayerBackend();
        var right = new FakePlayerBackend();
        var detector = new FakeVisionDetector([Body(.3)]);
        await using var panel = new PlayerPanelViewModel(left, visionDetector: detector);
        _ = new PlayerPanelViewModel(right);

        await panel.StartTrackingAsync();
        await detector.Called.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await WaitFor(() => left.Calls.Any(call => call.StartsWith("transform:")));
        await panel.ZoomWheelAsync(120, new NormalizedPoint(.5, .5));

        Assert.False(panel.IsTracking);
        Assert.Empty(right.Calls);
    }

    [Fact]
    public async Task MultiplePeople_WaitForVideoClick()
    {
        var backend = new FakePlayerBackend();
        var detector = new FakeVisionDetector([Body(.2), Body(.8)]);
        await using var panel = new PlayerPanelViewModel(backend, visionDetector: detector);

        await panel.StartTrackingAsync();
        await WaitFor(() => panel.IsSubjectSelectionRequired);
        Assert.DoesNotContain(backend.Calls, call => call.StartsWith("transform:"));

        await panel.SelectSubjectAsync(new NormalizedPoint(.75, .5));
        Assert.Contains(backend.Calls, call => call.StartsWith("transform:"));
    }

    [Fact]
    public async Task Tracking_RetriesWhenFirstFrameIsNotReady()
    {
        var backend = new FakePlayerBackend { CaptureFailures = 1 };
        await using var panel = new PlayerPanelViewModel(backend, visionDetector: new FakeVisionDetector([Body(.5)]));

        await panel.StartTrackingAsync();
        await WaitFor(() => backend.Calls.Any(call => call.StartsWith("transform:")));

        Assert.True(panel.IsTracking);
    }

    private sealed class FakeSeparation(string? result = null) : IAudioSeparationService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<string> GetOrCreateVoiceCacheAsync(string sourcePath,
            IProgress<AudioProcessingProgress>? progress, CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            if (result is not null) return result;
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return "";
        }
        public Task<string> GetOrCreateDetailCacheAsync(string sourcePath,
            IProgress<AudioProcessingProgress>? progress, CancellationToken cancellationToken) =>
            GetOrCreateVoiceCacheAsync(sourcePath, progress, cancellationToken);
    }

    private static PoseCandidate Body(double x)
    {
        var points = Enumerable.Repeat(new PoseLandmark(0, 0, 0), 33).ToArray();
        foreach (var index in new[] { 0, 11, 12, 23, 24, 27, 28 }) points[index] = new(x, .2 + index / 50d, 1);
        return new(points);
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        for (var i = 0; i < 20 && !condition(); i++) await Task.Delay(25);
        Assert.True(condition());
    }

    private sealed class FakeVisionDetector(IReadOnlyList<PoseCandidate> poses) : IVisionDetector
    {
        public TaskCompletionSource Called { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IReadOnlyList<PoseCandidate>> DetectAsync(VideoFrame frame, CancellationToken cancellationToken)
        {
            Called.TrySetResult();
            return Task.FromResult(poses);
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
