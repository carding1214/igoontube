namespace PUPlayer.Core.Playback;

public readonly record struct PlaybackLoop(double? Start = null, double? End = null)
{
    public bool IsActive => Start is >= 0 && End > Start;
    public PlaybackLoop WithStart(double seconds) => this with { Start = Math.Max(0, seconds) };
    public PlaybackLoop WithEnd(double seconds) => this with { End = Math.Max(0, seconds) };
    public double? SeekTarget(double position) => IsActive && position >= End ? Start : null;
}
