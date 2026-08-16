using System.Text.RegularExpressions;

namespace PUPlayer.Core.Settings;

public enum ThemePreset { CarbonBlue, MidnightSoft, GraphiteCompact, Custom }
public enum ControlDensity { Compact, Comfortable }

public sealed record AppSettings(int Version, string Language, ThemePreset ThemePreset, string AccentColor,
    int ButtonCornerRadius, int ControlHeight, ControlDensity Density, string? LastExportDirectory)
{
    public static AppSettings Default { get; } = new(1, "en", ThemePreset.CarbonBlue, "#2F8CFF", 7, 32, ControlDensity.Compact, null);

    public AppSettings ApplyPreset(ThemePreset preset) => preset switch
    {
        ThemePreset.CarbonBlue => Default,
        ThemePreset.MidnightSoft => this with { ThemePreset = preset, AccentColor = "#60A5FA", ButtonCornerRadius = 18, ControlHeight = 34, Density = ControlDensity.Comfortable },
        ThemePreset.GraphiteCompact => this with { ThemePreset = preset, AccentColor = "#1683E8", ButtonCornerRadius = 3, ControlHeight = 28, Density = ControlDensity.Compact },
        _ => this
    };

    public AppSettings Normalize()
    {
        var accent = AccentColor;
        if (Version != 1 || Language is not ("en" or "es") || !Enum.IsDefined(ThemePreset) ||
            accent is null || !Regex.IsMatch(accent, "^#[0-9A-Fa-f]{6}$") || ButtonCornerRadius is < 0 or > 20 ||
            ControlHeight is < 28 or > 40 || !Enum.IsDefined(Density)) return Default;
        var directory = LastExportDirectory is { } path && Path.IsPathFullyQualified(path) && Directory.Exists(path)
            ? Path.GetFullPath(path) : null;
        return this with { AccentColor = accent.ToUpperInvariant(), LastExportDirectory = directory };
    }
}
