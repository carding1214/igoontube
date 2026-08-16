using PUPlayer.Core.Playback;
using PUPlayer.MpvWorker.Interop;

namespace PUPlayer.MpvWorker.Worker;

public sealed class PlayerWorker : IDisposable
{
    private readonly IMpvClient mpv;
    public bool IsShutdown { get; private set; }

    public PlayerWorker(IMpvClient mpv) => this.mpv = mpv;

    public ValueTask<PlayerEvent?> ApplyAsync(PlayerRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        switch (request)
        {
            case PlayerRequest.Load load:
                if (!LocalMediaPath.TryCreate(load.Path, out var path))
                    throw new ArgumentException("Only existing absolute local files are allowed.", nameof(request));
                mpv.Load(path.Value);
                break;
            case PlayerRequest.SetPause pause: mpv.SetPaused(pause.Value); break;
            case PlayerRequest.Seek seek: mpv.Seek(seek.Seconds); break;
            case PlayerRequest.SetVolume volume: mpv.SetVolume(volume.Percent); break;
            case PlayerRequest.SetSpeed speed: mpv.SetSpeed(speed.Value); break;
            case PlayerRequest.SetTransform transform: mpv.SetTransform(transform.Value); break;
            case PlayerRequest.SetGeometry geometry: mpv.SetGeometry(geometry.Value); break;
            case PlayerRequest.SetAudioFilter filter: mpv.SetAudioFilter(filter.Value); break;
            case PlayerRequest.LoadExternalAudio audio:
                if (!LocalMediaPath.TryCreate(audio.Path, out var audioPath))
                    throw new ArgumentException("Only existing absolute local audio files are allowed.", nameof(request));
                mpv.LoadExternalAudio(audioPath.Value);
                break;
            case PlayerRequest.UseOriginalAudio: mpv.UseOriginalAudio(); break;
            case PlayerRequest.CaptureFrame capture:
                return ValueTask.FromResult<PlayerEvent?>(new PlayerEvent.FrameCaptured(capture.Id, mpv.CaptureFrame(capture.MaxWidth)));
            case PlayerRequest.Shutdown:
                IsShutdown = true;
                mpv.Dispose();
                break;
            default: throw new ArgumentOutOfRangeException(nameof(request));
        }
        return ValueTask.FromResult<PlayerEvent?>(null);
    }

    public PlayerSnapshot ReadSnapshot() => mpv.ReadSnapshot();
    public void Dispose() => mpv.Dispose();
}
