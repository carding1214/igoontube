using System.Windows;
using System.Windows.Media;
using System.Runtime.ExceptionServices;
using PUPlayer.App.Personalization;
using PUPlayer.App.Views;
using PUPlayer.Core.Settings;

namespace PUPlayer.IntegrationTests.UI;

public sealed class PersonalizationTests
{
    [Fact]
    public void Languages_ExposeIdenticalKeys()
    {
        var en = Load("Localization/Strings.en.xaml");
        var es = Load("Localization/Strings.es.xaml");
        Assert.Equal(en.Keys.Cast<object>().OrderBy(x => x.ToString()), es.Keys.Cast<object>().OrderBy(x => x.ToString()));
        Assert.Equal("Settings", en["Settings"]);
        Assert.Equal("Ajustes", es["Settings"]);
    }

    [Fact]
    public void CarbonBlue_AppliesApprovedTokens()
    {
        var resources = new ResourceDictionary();
        new ThemeService(resources).Apply(AppSettings.Default);
        Assert.Equal(Color.FromRgb(0x2F, 0x8C, 0xFF), ((SolidColorBrush)resources["AccentBrush"]).Color);
        Assert.Equal(new CornerRadius(7), resources["ButtonCornerRadius"]);
        Assert.Equal(32d, resources["ControlHeight"]);
    }

    [Fact]
    public void Localization_CanSwitchAtRuntime()
    {
        var resources = new ResourceDictionary();
        var service = new LocalizationService(resources);
        service.Apply("es");
        Assert.Equal("Ajustes", service.Text("Settings"));
        service.Apply("en");
        Assert.Equal("Settings", service.Text("Settings"));
    }

    [Fact]
    public void Workspace_ExposesCompactSettingsPopup()
    {
        RunSta(() =>
        {
            var view = new WorkspaceView();
            Assert.NotNull(view.FindName("SettingsToggle"));
            Assert.NotNull(view.FindName("SettingsPopup"));
            Assert.NotNull(view.FindName("AccentInput"));
        });
    }

    private static ResourceDictionary Load(string path) => (ResourceDictionary)Application.LoadComponent(
        new Uri($"/IgoonTube;component/{path}", UriKind.Relative));

    private static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { action(); } catch (Exception exception) { error = exception; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(); thread.Join();
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}
