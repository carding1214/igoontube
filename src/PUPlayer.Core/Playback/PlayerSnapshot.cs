namespace PUPlayer.Core.Playback;

public sealed record PlayerSnapshot(
    double PositionSeconds,
    double DurationSeconds,
    bool Paused,
    double Speed,
    double VolumePercent);
