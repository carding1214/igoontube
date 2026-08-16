using PUPlayer.Core.Playback;
using PUPlayer.Core.Zoom;
using PUPlayer.Core.MediaTools;

namespace PUPlayer.App.Playback;

public interface IPlayerBackend : IAsyncDisposable
{
    IAsyncEnumerable<PlayerSnapshot> Snapshots(CancellationToken cancellationToken);
    Task LoadAsync(string path, nint windowHandle, CancellationToken cancellationToken);
    Task SetPausedAsync(bool value, CancellationToken cancellationToken);
    Task SeekAsync(double seconds, CancellationToken cancellationToken);
    Task SetVolumeAsync(double percent, CancellationToken cancellationToken);
    Task SetSpeedAsync(double speed, CancellationToken cancellationToken);
    Task SetTransformAsync(MpvTransform transform, CancellationToken cancellationToken);
    Task SetGeometryAsync(VideoTransform geometry, CancellationToken cancellationToken);
    Task SetAudioFilterAsync(string value, CancellationToken cancellationToken);
    Task LoadExternalAudioAsync(string path, CancellationToken cancellationToken);
    Task UseOriginalAudioAsync(CancellationToken cancellationToken);
    Task<VideoFrame> CaptureFrameAsync(CancellationToken cancellationToken);
}
