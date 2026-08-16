namespace PUPlayer.Core.Fullscreen;

public sealed class FullscreenState
{
    private DateTimeOffset hideAt;
    public bool IsActive { get; private set; }
    public bool AreControlsVisible { get; private set; } = true;

    public void Enter(DateTimeOffset now) { IsActive = true; Move(now); }
    public void Move(DateTimeOffset now) { if (IsActive) { AreControlsVisible = true; hideAt = now.AddSeconds(2); } }
    public void Tick(DateTimeOffset now) { if (IsActive && now >= hideAt) AreControlsVisible = false; }
    public void Exit() { IsActive = false; AreControlsVisible = true; }
}
