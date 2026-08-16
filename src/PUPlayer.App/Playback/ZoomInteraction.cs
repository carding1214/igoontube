using PUPlayer.Core.Zoom;

namespace PUPlayer.App.Playback;

public sealed class ZoomInteraction
{
    public ZoomState State { get; private set; } = ZoomState.Default;

    public void Wheel(int delta, NormalizedPoint cursor) =>
        State = State.ZoomAt(Math.Pow(1.15, delta / 120d), cursor);

    public void Drag(double dx, double dy) => State = State.PanBy(dx, dy);
    public void Reset() => State = ZoomState.Default;
}
