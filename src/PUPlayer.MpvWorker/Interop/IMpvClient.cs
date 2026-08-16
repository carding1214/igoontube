using PUPlayer.Core.Playback;
using PUPlayer.Core.Zoom;
using PUPlayer.Core.MediaTools;

namespace PUPlayer.MpvWorker.Interop;

public interface IMpvClient : IDisposable
{
    void Load(string path);
    void SetPaused(bool value);
    void Seek(double seconds);
    void SetVolume(double percent);
    void SetSpeed(double value);
    void SetTransform(MpvTransform value);
    void SetGeometry(VideoTransform value);
    void SetAudioFilter(string value);
    void LoadExternalAudio(string path);
    void UseOriginalAudio();
    VideoFrame CaptureFrame(int maxWidth);
    PlayerSnapshot ReadSnapshot();
}
