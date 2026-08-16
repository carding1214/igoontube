using System.Runtime.CompilerServices;
using PUPlayer.App.Playback;
using PUPlayer.Core.Playback;
using PUPlayer.Core.Zoom;
using PUPlayer.Core.MediaTools;
using System.Threading.Channels;

namespace PUPlayer.IntegrationTests.Fakes;

public sealed class FakePlayerBackend : IPlayerBackend
{
    private readonly Channel<PlayerSnapshot> snapshots = Channel.CreateUnbounded<PlayerSnapshot>();
    public List<string> Calls { get; } = [];
    public int CaptureFailures { get; set; }
    public async IAsyncEnumerable<PlayerSnapshot> Snapshots([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var snapshot in snapshots.Reader.ReadAllAsync(cancellationToken)) yield return snapshot;
    }
    public void Publish(PlayerSnapshot snapshot) => snapshots.Writer.TryWrite(snapshot);
    public Task LoadAsync(string path, nint windowHandle, CancellationToken cancellationToken) { Calls.Add($"load:{path}"); return Task.CompletedTask; }
    public Task SetPausedAsync(bool value, CancellationToken cancellationToken) { Calls.Add($"pause:{value}"); return Task.CompletedTask; }
    public Task SeekAsync(double value, CancellationToken cancellationToken) { Calls.Add($"seek:{value}"); return Task.CompletedTask; }
    public Task SetVolumeAsync(double value, CancellationToken cancellationToken) { Calls.Add($"volume:{value}"); return Task.CompletedTask; }
    public Task SetSpeedAsync(double value, CancellationToken cancellationToken) { Calls.Add($"speed:{value}"); return Task.CompletedTask; }
    public Task SetTransformAsync(MpvTransform value, CancellationToken cancellationToken) { Calls.Add($"transform:{value}"); return Task.CompletedTask; }
    public Task SetGeometryAsync(VideoTransform value, CancellationToken cancellationToken) { Calls.Add($"geometry:{value}"); return Task.CompletedTask; }
    public Task SetAudioFilterAsync(string value, CancellationToken cancellationToken) { Calls.Add($"filter:{value}"); return Task.CompletedTask; }
    public Task LoadExternalAudioAsync(string path, CancellationToken cancellationToken) { Calls.Add($"audio:{path}"); return Task.CompletedTask; }
    public Task UseOriginalAudioAsync(CancellationToken cancellationToken) { Calls.Add("audio:original"); return Task.CompletedTask; }
    public Task<VideoFrame> CaptureFrameAsync(CancellationToken cancellationToken)
    {
        if (CaptureFailures-- > 0) throw new InvalidOperationException("frame not ready");
        return Task.FromResult(new VideoFrame(1, 1, [0, 0, 0]));
    }
    public ValueTask DisposeAsync() { snapshots.Writer.TryComplete(); return ValueTask.CompletedTask; }
}
