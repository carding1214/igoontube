using PUPlayer.Core.Settings;

namespace PUPlayer.Core.Tests.Settings;

public sealed class AppSettingsTests
{
    [Fact]
    public void Defaults_AreEnglishCarbonBlue() =>
        Assert.Equal(new(1, "en", ThemePreset.CarbonBlue, "#2F8CFF", 7, 32, ControlDensity.Compact, null), AppSettings.Default);

    [Theory]
    [InlineData(ThemePreset.MidnightSoft, "#60A5FA", 18, 34, ControlDensity.Comfortable)]
    [InlineData(ThemePreset.GraphiteCompact, "#1683E8", 3, 28, ControlDensity.Compact)]
    public void Preset_AppliesExactTokens(ThemePreset preset, string accent, int radius, int height, ControlDensity density)
    {
        var value = AppSettings.Default.ApplyPreset(preset);
        Assert.Equal((accent, radius, height, density), (value.AccentColor, value.ButtonCornerRadius, value.ControlHeight, value.Density));
    }

    [Fact]
    public void Normalize_RejectsInvalidValues()
    {
        var value = new AppSettings(99, "fr", ThemePreset.Custom, "blue", -5, 99, (ControlDensity)99, @"Z:\missing").Normalize();
        Assert.Equal(AppSettings.Default, value);
    }
}
