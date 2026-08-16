using System.Threading;
using System.Windows;
using PUPlayer.Core.Playback;

namespace PUPlayer.App;

public partial class App : Application
{
    private EventWaitHandle? ready;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args is not [var value] || !LocalMediaPath.TryCreate(value, out var path))
        {
            Shutdown(2);
            return;
        }

        ready = new(false, EventResetMode.ManualReset, $"IgoonTube.Ready.{Environment.ProcessId}");
        var metrics = new PlayerLoadMetrics();
        var window = new MainWindow(path.Value, metrics);
        window.Loaded += (_, _) => ready.Set();
        window.ContentRendered += (_, _) => metrics.MarkWindowVisible();
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ready?.Dispose();
        base.OnExit(e);
    }
}
