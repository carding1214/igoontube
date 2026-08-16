using PUPlayer.Core.Audio;

namespace PUPlayer.Core.Tests.Audio;

public sealed class MpvAudioFilterBuilderTests
{
    [Fact]
    public void VoicePreset_BuildsLocalLavfiChain()
    {
        var value = MpvAudioFilterBuilder.Build(AudioSettings.FromPreset(AudioPreset.Voice));

        Assert.Contains("highpass=f=70", value);
        Assert.Contains("equalizer=f=1200", value);
        Assert.DoesNotContain("http", value, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManualValues_AreClamped()
    {
        var settings = new AudioSettings(999, 99, -99, 2, true);

        Assert.Equal(new AudioSettings(180, 12, -12, 1, true), settings.Clamp());
    }

    [Fact]
    public void DetailedIntimatePreset_PreservesDetailWithoutDenoise()
    {
        var settings = AudioSettings.FromPreset(AudioPreset.DetailedIntimate);

        Assert.Equal(new AudioSettings(35, 4, 6, 0, true), settings);
        var filter = MpvAudioFilterBuilder.Build(settings);
        Assert.Contains("highpass=f=35", filter);
        Assert.Contains("equalizer=f=3500", filter);
        Assert.DoesNotContain("afftdn", filter);
    }
}
