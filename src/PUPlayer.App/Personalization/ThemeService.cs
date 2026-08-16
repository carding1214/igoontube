using System.Windows;
using System.Windows.Media;
using PUPlayer.Core.Settings;

namespace PUPlayer.App.Personalization;

public sealed class ThemeService(ResourceDictionary resources)
{
    public void Apply(AppSettings settings)
    {
        settings = settings.Normalize();
        var color = (Color)ColorConverter.ConvertFromString(settings.AccentColor);
        resources["AccentBrush"] = Frozen(color);
        resources["AccentHoverBrush"] = Frozen(Color.FromRgb(Light(color.R), Light(color.G), Light(color.B)));
        resources["BrandBlueBrush"] = Frozen(color);
        resources["ButtonCornerRadius"] = new CornerRadius(settings.ButtonCornerRadius);
        resources["ControlHeight"] = (double)settings.ControlHeight;
        resources["ControlSpacing"] = settings.Density == ControlDensity.Compact ? new Thickness(3) : new Thickness(5);
    }

    private static byte Light(byte value) => (byte)Math.Min(255, value + 30);
    private static SolidColorBrush Frozen(Color color) { var brush = new SolidColorBrush(color); brush.Freeze(); return brush; }
}
