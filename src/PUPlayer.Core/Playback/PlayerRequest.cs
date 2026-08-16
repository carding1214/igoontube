using System.Text.Json.Serialization;
using PUPlayer.Core.Zoom;
using PUPlayer.Core.MediaTools;

namespace PUPlayer.Core.Playback;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Load), "load")]
[JsonDerivedType(typeof(SetPause), "pause")]
[JsonDerivedType(typeof(Seek), "seek")]
[JsonDerivedType(typeof(SetVolume), "volume")]
[JsonDerivedType(typeof(SetSpeed), "speed")]
[JsonDerivedType(typeof(SetTransform), "transform")]
[JsonDerivedType(typeof(SetGeometry), "geometry")]
[JsonDerivedType(typeof(SetAudioFilter), "audio-filter")]
[JsonDerivedType(typeof(LoadExternalAudio), "external-audio")]
[JsonDerivedType(typeof(UseOriginalAudio), "original-audio")]
[JsonDerivedType(typeof(CaptureFrame), "capture-frame")]
[JsonDerivedType(typeof(Shutdown), "shutdown")]
public abstract record PlayerRequest(long Id)
{
    public sealed record Load(long Id, string Path) : PlayerRequest(Id)
    {
        public static Load Create(long id, string path)
        {
            if (!LocalMediaPath.TryCreate(path, out var local))
                throw new ArgumentException("Only absolute local paths are allowed.", nameof(path));
            return new(id, local.Value);
        }
    }

    public sealed record SetPause(long Id, bool Value) : PlayerRequest(Id);
    public sealed record Seek(long Id, double Seconds) : PlayerRequest(Id);
    public sealed record SetVolume(long Id, double Percent) : PlayerRequest(Id);
    public sealed record SetSpeed(long Id, double Value) : PlayerRequest(Id);
    public sealed record SetTransform(long Id, MpvTransform Value) : PlayerRequest(Id);
    public sealed record SetGeometry(long Id, VideoTransform Value) : PlayerRequest(Id);
    public sealed record SetAudioFilter(long Id, string Value) : PlayerRequest(Id);
    public sealed record LoadExternalAudio(long Id, string Path) : PlayerRequest(Id);
    public sealed record UseOriginalAudio(long Id) : PlayerRequest(Id);
    public sealed record CaptureFrame(long Id, int MaxWidth = 384) : PlayerRequest(Id);
    public sealed record Shutdown(long Id) : PlayerRequest(Id);
}
