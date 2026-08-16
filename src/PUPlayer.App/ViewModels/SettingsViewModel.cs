using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using PUPlayer.Core.Settings;

namespace PUPlayer.App.ViewModels;

public sealed class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly AppSettingsStore store;
    private readonly Action<AppSettings> apply;
    private readonly Action<Action> dispatch;
    private AppSettings value;
    private string? errorKey;

    public SettingsViewModel(AppSettingsStore store, Action<AppSettings> apply, Action<Action>? dispatch = null)
    {
        this.store = store;
        this.apply = apply;
        this.dispatch = dispatch ?? (action => action());
        value = store.Load();
        store.Changed += Store_Changed;
        apply(value);
    }

    public AppSettings Value => value;
    public string Language { get => value.Language; set { if (value != this.value.Language && value is "en" or "es") Save(this.value with { Language = value }); } }
    public ThemePreset ThemePreset { get => value.ThemePreset; set { if (value != this.value.ThemePreset) SelectPreset(value); } }
    public string AccentColor
    {
        get => value.AccentColor;
        set
        {
            var accent = value;
            if (accent is null || !Regex.IsMatch(accent, "^#[0-9A-Fa-f]{6}$")) { ErrorKey = "InvalidColor"; return; }
            ErrorKey = null;
            Save(this.value with { ThemePreset = ThemePreset.Custom, AccentColor = accent.ToUpperInvariant() });
        }
    }
    public int ButtonCornerRadius { get => value.ButtonCornerRadius; set => Save(this.value with { ThemePreset = ThemePreset.Custom, ButtonCornerRadius = Math.Clamp(value, 0, 20) }); }
    public int ControlHeight { get => value.ControlHeight; set => Save(this.value with { ThemePreset = ThemePreset.Custom, ControlHeight = Math.Clamp(value, 28, 40) }); }
    public ControlDensity Density { get => value.Density; set => Save(this.value with { ThemePreset = ThemePreset.Custom, Density = value }); }
    public string? ErrorKey { get => errorKey; private set => SetProperty(ref errorKey, value); }

    public void SelectPreset(ThemePreset preset)
    {
        if (preset == ThemePreset.Custom) return;
        Save(value.ApplyPreset(preset) with { Language = value.Language, LastExportDirectory = value.LastExportDirectory });
    }

    public void RestorePreset() => SelectPreset(value.ThemePreset == ThemePreset.Custom ? ThemePreset.CarbonBlue : value.ThemePreset);
    public void SetLastExportDirectory(string? directory) => Save(value with { LastExportDirectory = directory });

    private void Save(AppSettings next)
    {
        next = next.Normalize();
        if (next == value) return;
        store.Save(next);
        SetValue(next);
    }

    private void Store_Changed(AppSettings next) => dispatch(() => SetValue(next));
    private void SetValue(AppSettings next)
    {
        if (next == value) return;
        value = next;
        apply(next);
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(Language));
        OnPropertyChanged(nameof(ThemePreset));
        OnPropertyChanged(nameof(AccentColor));
        OnPropertyChanged(nameof(ButtonCornerRadius));
        OnPropertyChanged(nameof(ControlHeight));
        OnPropertyChanged(nameof(Density));
    }

    public void Dispose() => store.Changed -= Store_Changed;
}
