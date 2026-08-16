using System.IO;
using PUPlayer.App.ViewModels;
using PUPlayer.Core.Settings;

namespace PUPlayer.IntegrationTests.UI;

public sealed class SettingsViewModelTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "IgoonTube-settings-ui-" + Guid.NewGuid().ToString("N"));
    private string PathName => Path.Combine(root, "settings.json");

    [Fact]
    public void ManualAccent_BecomesCustomAndPersists()
    {
        using var store = new AppSettingsStore(PathName);
        using var vm = new SettingsViewModel(store, _ => { });
        vm.AccentColor = "#0055ff";
        Assert.Equal(ThemePreset.Custom, vm.Value.ThemePreset);
        Assert.Equal("#0055FF", store.Load().AccentColor);
    }

    [Fact]
    public void InvalidAccent_PreservesValueAndReportsError()
    {
        using var store = new AppSettingsStore(PathName);
        using var vm = new SettingsViewModel(store, _ => { });
        vm.AccentColor = "blue";
        Assert.Equal("#2F8CFF", vm.AccentColor);
        Assert.Equal("InvalidColor", vm.ErrorKey);
    }

    [Fact]
    public async Task AnotherStore_UpdatesTheViewModel()
    {
        using var store = new AppSettingsStore(PathName);
        using var other = new AppSettingsStore(PathName);
        using var vm = new SettingsViewModel(store, _ => { });
        other.Save(AppSettings.Default with { Language = "es" });
        await Task.Delay(150);
        Assert.Equal("es", vm.Language);
    }

    public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
