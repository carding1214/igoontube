using System.Windows;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using PUPlayer.App.Views;
using PUPlayer.App.Playback;
using PUPlayer.IntegrationTests.App;

namespace PUPlayer.IntegrationTests.UI;

public sealed class CarbonThemeTests
{
    [Fact]
    public void AppProject_UsesIgoonTubeBrandAssets()
    {
        var root = TestPaths.Repository;
        var project = File.ReadAllText(Path.Combine(root, "src", "PUPlayer.App", "PUPlayer.App.csproj"));

        Assert.Contains("<AssemblyName>IgoonTube</AssemblyName>", project);
        Assert.Contains("<ApplicationIcon>Assets\\IgoonTube.ico</ApplicationIcon>", project);
        Assert.True(File.Exists(Path.Combine(root, "src", "PUPlayer.App", "Assets", "IgoonTube.png")));
        Assert.True(File.Exists(Path.Combine(root, "src", "PUPlayer.App", "Assets", "IgoonTube.ico")));
        var window = File.ReadAllText(Path.Combine(root, "src", "PUPlayer.App", "MainWindow.xaml"));
        Assert.DoesNotContain("Icon=\"Assets/IgoonTube.ico\"", window);
    }

    [Fact]
    public void CarbonTheme_ExposesSharedControlStyles()
    {
        var theme = (ResourceDictionary)Application.LoadComponent(
            new Uri("/IgoonTube;component/Themes/CarbonControls.xaml", UriKind.Relative));

        foreach (var key in new[]
        {
            "CarbonButton", "CarbonIconButton", "CarbonPrimaryButton", "CarbonComboBox",
            "CarbonSlider", "CarbonCheckBox", "CarbonExpander"
        })
            Assert.True(theme.Contains(key), $"Missing {key}");

        Assert.Equal(Color.FromRgb(0x2F, 0x8C, 0xFF), ((SolidColorBrush)theme["BrandBlueBrush"]).Color);
        Assert.Equal(Color.FromRgb(0xF1, 0xF5, 0xF7), ((SolidColorBrush)theme["BrandWhiteBrush"]).Color);
    }

    [Fact]
    public void CarbonExpander_CanMeasureItsTemplate()
    {
        RunSta(() =>
        {
            var theme = (ResourceDictionary)Application.LoadComponent(
                new Uri("/IgoonTube;component/Themes/CarbonControls.xaml", UriKind.Relative));
            var expander = new System.Windows.Controls.Expander
            {
                Header = "Ajuste manual",
                Content = new System.Windows.Controls.TextBlock { Text = "Audio" },
                IsExpanded = true,
                Style = (Style)theme["CarbonExpander"]
            };

            expander.Measure(new Size(400, 200));

            Assert.True(expander.DesiredSize.Width > 0);
        });
    }

    [Fact]
    public void CompactToolbar_UsesOneHorizontalLineAndPopupSettings()
    {
        RunSta(() =>
        {
            var view = new PlayerPanelView();

            var strip = Assert.IsType<StackPanel>(view.FindName("ControlStrip"));
            Assert.Equal(Orientation.Horizontal, strip.Orientation);
            Assert.IsType<Popup>(view.FindName("ManualAudioPopup"));
        });
    }

    [Fact]
    public void ExportProgress_IsOneWayToReadOnlyViewModelProperty()
    {
        var xaml = File.ReadAllText(Path.Combine(TestPaths.Repository, "src", "PUPlayer.App", "Views", "PlayerPanelView.xaml"));

        Assert.Contains("Value=\"{Binding ExportProgress, Mode=OneWay}\"", xaml);
    }

    [Fact]
    public void PlayerPanel_HasNonBlockingLoadingOverlay()
    {
        var xaml = File.ReadAllText(Path.Combine(TestPaths.Repository, "src", "PUPlayer.App", "Views", "PlayerPanelView.xaml"));

        Assert.Contains("x:Name=\"LoadingOverlay\"", xaml);
        Assert.Contains("Binding IsLoading", xaml);
        Assert.Contains("Text=\"{Binding LoadStatus}\"", xaml);
        Assert.Contains("IsHitTestVisible=\"False\"", xaml);
    }

    [Fact]
    public void Workspace_UsesPersistentGridPanelHost()
    {
        RunSta(() =>
        {
            var view = new WorkspaceView();
            Assert.IsType<Grid>(view.FindName("PanelsHost"));
        });
    }

    [Fact]
    public void Workspace_ShowsIgoonTubeWordmark()
    {
        RunSta(() =>
        {
            var view = new WorkspaceView();
            Assert.IsType<StackPanel>(view.FindName("BrandWordmark"));
        });
    }

    [Fact]
    public void MpvSurface_MapsNativeDoubleClick()
    {
        RunSta(() =>
        {
            var surface = new MpvSurface();
            var calls = 0;
            surface.DoubleClicked += () => calls++;
            var method = typeof(MpvSurface).GetMethod("WndProc", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

            method.Invoke(surface, [nint.Zero, 0x0203, nint.Zero, nint.Zero, false]);

            Assert.Equal(1, calls);
        });
    }

    [Fact]
    public void MpvSurface_DoesNotRepeatIdenticalMouseActivity()
    {
        RunSta(() =>
        {
            var surface = new MpvSurface();
            var calls = 0;
            surface.MouseMoved += () => calls++;
            var method = typeof(MpvSurface).GetMethod("WndProc", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            object[] message = [nint.Zero, 0x0200, nint.Zero, (nint)((100 << 16) | 100), false];

            method.Invoke(surface, message);
            method.Invoke(surface, message);

            Assert.Equal(1, calls);
        });
    }

    private static void RunSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception exception) { error = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }
}
