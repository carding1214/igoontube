using System.Text.Json.Serialization;

namespace PUPlayer.Core.Playback;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Ready), "ready")]
[JsonDerivedType(typeof(SnapshotChanged), "snapshot")]
[JsonDerivedType(typeof(Ended), "ended")]
[JsonDerivedType(typeof(Failed), "error")]
[JsonDerivedType(typeof(FrameCaptured), "frame")]
public abstract record PlayerEvent
{
    public sealed record Ready : PlayerEvent;
    public sealed record SnapshotChanged(PlayerSnapshot Value) : PlayerEvent;
    public sealed record Ended : PlayerEvent;
    public sealed record Failed(string Code, string Message, long? RequestId = null) : PlayerEvent;
    public sealed record FrameCaptured(long RequestId, VideoFrame Frame) : PlayerEvent;
}
