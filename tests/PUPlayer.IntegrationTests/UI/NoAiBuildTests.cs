using PUPlayer.App.Features;
using PUPlayer.App.ViewModels;
using PUPlayer.IntegrationTests.Fakes;
using System.Windows;

namespace PUPlayer.IntegrationTests.UI;

public sealed class NoAiBuildTests
{
    [Fact]
    public void CompiledBuild_ReportsItsAiCapability()
    {
#if IGOONTUBE_NO_AI
        Assert.False(BuildCapabilities.AiAvailable);
#else
        Assert.True(BuildCapabilities.AiAvailable);
#endif
    }

    [Fact]
    public void Panel_CollapsesAiControlsWhenUnavailable()
    {
        var panel = new PlayerPanelViewModel(new FakePlayerBackend());
#if IGOONTUBE_NO_AI
        Assert.Equal(Visibility.Collapsed, panel.AiFeaturesVisibility);
#else
        Assert.Equal(Visibility.Visible, panel.AiFeaturesVisibility);
#endif
    }
}
