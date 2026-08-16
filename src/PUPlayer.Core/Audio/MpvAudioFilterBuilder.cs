using System.Globalization;

namespace PUPlayer.Core.Audio;

public static class MpvAudioFilterBuilder
{
    public static string Build(AudioSettings value)
    {
        var settings = value.Clamp();
        var filters = new List<string>();
        if (settings.LowCutHz > 0) filters.Add($"highpass=f={Format(settings.LowCutHz)}");
        if (settings.VoiceGainDb != 0) filters.Add($"equalizer=f=1200:t=q:w=1:g={Format(settings.VoiceGainDb)}");
        if (settings.PresenceGainDb != 0) filters.Add($"equalizer=f=3500:t=q:w=1:g={Format(settings.PresenceGainDb)}");
        if (settings.Denoise > 0) filters.Add($"afftdn=nr={Format(6 + 24 * settings.Denoise)}:nf=-50");
        if (settings.Compression) filters.Add("acompressor=threshold=0.125:ratio=3:attack=20:release=250:makeup=2");
        return filters.Count == 0 ? string.Empty : $"lavfi=[{string.Join(',', filters)}]";
    }

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
