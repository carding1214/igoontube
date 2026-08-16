namespace PUPlayer.Core.Audio;

public sealed record AudioSettings(
    double LowCutHz,
    double VoiceGainDb,
    double PresenceGainDb,
    double Denoise,
    bool Compression)
{
    public static AudioSettings Natural { get; } = new(0, 0, 0, 0, false);

    public static AudioSettings FromPreset(AudioPreset preset) => preset switch
    {
        AudioPreset.Natural => Natural,
        AudioPreset.Voice => new(70, 5, 2, .15, true),
        AudioPreset.Intimate => new(45, 3, 5, .05, true),
        AudioPreset.DetailedIntimate => new(35, 4, 6, 0, true),
        AudioPreset.Denoise => new(80, 2, 0, .6, true),
        _ => throw new ArgumentOutOfRangeException(nameof(preset))
    };

    public AudioSettings Clamp() => new(
        Math.Clamp(LowCutHz, 0, 180),
        Math.Clamp(VoiceGainDb, -12, 12),
        Math.Clamp(PresenceGainDb, -12, 12),
        Math.Clamp(Denoise, 0, 1),
        Compression);
}
