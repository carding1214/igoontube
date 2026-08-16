using System.Diagnostics;
using PUPlayer.App.Playback;
using PUPlayer.Core.Tracking;
using PUPlayer.Core.Zoom;
using PUPlayer.App.Personalization;

namespace PUPlayer.App.Tracking;

public sealed record VisionUpdate(MpvTransform? Transform, string Status, bool NeedsSelection);

public sealed class VisionCoordinator(IPlayerBackend backend, IVisionDetector detector, ITextProvider? text = null) : IAsyncDisposable
{
    private readonly AutoFrameTracker tracker = new();
    private CancellationTokenSource? cancellation;
    private Task? loop;
    private IReadOnlyList<PoseCandidate> candidates = [];
    private readonly Stopwatch clock = Stopwatch.StartNew();

    public event Action<VisionUpdate>? Updated;
    public bool IsRunning => loop is { IsCompleted: false };

    public Task StartAsync()
    {
        if (IsRunning) return Task.CompletedTask;
        cancellation = new();
        loop = RunAsync(cancellation.Token);
        Updated?.Invoke(new(null, T("SearchingPerson", "Buscando persona…"), false));
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (cancellation is null) return;
        cancellation.Cancel();
        if (loop is not null) try { await loop; } catch (OperationCanceledException) { }
        cancellation.Dispose();
        cancellation = null;
        loop = null;
    }

    public async Task SelectAsync(NormalizedPoint point, CancellationToken cancellationToken = default)
    {
        if (!IsRunning || candidates.Count < 2) return;
        tracker.Select(candidates, point);
        var transform = tracker.Update(candidates, clock.Elapsed);
        if (transform is not null) await backend.SetTransformAsync(transform, cancellationToken);
        Publish(transform);
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var started = clock.Elapsed;
                    var frame = await backend.CaptureFrameAsync(token);
                    candidates = await detector.DetectAsync(frame, token);
                    var transform = tracker.Update(candidates, clock.Elapsed);
                    if (transform is not null) await backend.SetTransformAsync(transform, token);
                    Publish(transform);
                    await Task.Delay(clock.Elapsed - started > TimeSpan.FromMilliseconds(180) ? 500 : 200, token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (Exception error)
                {
                    Updated?.Invoke(new(null, T("Retrying", "Reintentando: {0}", error.Message), false));
                    await Task.Delay(300, token);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    private void Publish(MpvTransform? transform)
    {
        var status = tracker.NeedsSelection ? T("SelectPerson", "Haz clic en una persona") : tracker.CurrentRegion switch
        {
            SubjectRegion.FullBody => T("FollowingBody", "Siguiendo cuerpo"),
            SubjectRegion.Torso => T("FollowingTorso", "Siguiendo torso"),
            SubjectRegion.Face => T("FollowingFace", "Siguiendo cara"),
            _ => T("SearchingPerson", "Buscando persona…")
        };
        Updated?.Invoke(new(transform, status, tracker.NeedsSelection));
    }

    private string T(string key, string fallback, params object[] args) => text?.Text(key, args) ?? string.Format(fallback, args);
}
