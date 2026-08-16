using System.Diagnostics;
using System.IO;

namespace PUPlayer.IntegrationTests.App;

internal sealed class TestApp : IDisposable
{
    private readonly Process process;
    private TestApp(Process process) => this.process = process;
    public int Id => process.Id;

    public static TestApp Start(string media)
    {
        var start = new ProcessStartInfo(TestPaths.AppExe) { UseShellExecute = false };
        start.ArgumentList.Add(media);
        start.Environment.Remove("DOTNET_ROOT");
        return new(Process.Start(start) ?? throw new InvalidOperationException("IgoonTube did not start."));
    }

    public async Task WaitForReady()
    {
        var limit = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < limit)
        {
            if (process.HasExited) throw new InvalidOperationException($"Process {Id} exited with {process.ExitCode}.");
            if (EventWaitHandle.TryOpenExisting($"IgoonTube.Ready.{Id}", out var ready))
            {
                ready.Dispose();
                return;
            }
            await Task.Delay(50);
        }
        throw new TimeoutException($"Process {Id} did not become ready.");
    }

    public void Dispose()
    {
        if (!process.HasExited)
        {
            process.CloseMainWindow();
            if (!process.WaitForExit(5_000)) process.Kill(true);
        }
        process.Dispose();
    }
}

internal static class TestPaths
{
    private static readonly string TestRoot = Environment.GetEnvironmentVariable("PUPLAYER_TEST_ROOT")
        ?? throw new InvalidOperationException("PUPLAYER_TEST_ROOT is missing.");
    private static string Configuration => new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
    public static string Repository => Directory.GetParent(Directory.GetParent(TestRoot)!.FullName)!.FullName;
    public static string AppExe => Path.Combine(Repository, "src", "PUPlayer.App", "bin", Configuration, "net8.0-windows", "win-x64", "IgoonTube.exe");
}
