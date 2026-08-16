using System.IO;
using System.Runtime.InteropServices;
using PUPlayer.IntegrationTests.App;
using PUPlayer.MpvWorker.Interop;
using PUPlayer.App.Playback;

namespace PUPlayer.IntegrationTests.Worker;

public sealed class MpvFrameCaptureTests
{
    [Fact]
    public void ScreenshotRaw_ReturnsDownsampledRgbInMemory()
    {
        var image = Path.Combine(Environment.GetEnvironmentVariable("PUPLAYER_TEST_ROOT")!, "frame.ppm");
        Directory.CreateDirectory(Path.GetDirectoryName(image)!);
        File.WriteAllBytes(image, "P6\n2 1\n255\n"u8.ToArray().Concat(new byte[] { 255, 0, 0, 0, 255, 0 }).ToArray());
        var window = CreateWindowEx(0, "STATIC", "", unchecked((int)0x80000000), 0, 0, 64, 64, 0, 0, 0, 0);
        Assert.NotEqual(0, window);
        try
        {
            var library = Path.Combine(TestPaths.Repository, "vendor", "mpv", "libmpv-2.dll");
            using var client = new MpvClient((ulong)window, library);
            client.Load(image);
            Thread.Sleep(400);

            var frame = client.CaptureFrame(1);

            Assert.Equal(1, frame.Width);
            Assert.Equal(3, frame.Rgb24.Length);
        }
        finally { DestroyWindow(window); }
    }

    [Fact]
    public async Task WorkerBackend_ReturnsCapturedFrame()
    {
        var image = Path.Combine(Environment.GetEnvironmentVariable("PUPLAYER_TEST_ROOT")!, "frame.ppm");
        var window = CreateWindowEx(0, "STATIC", "", unchecked((int)0x80000000), 0, 0, 64, 64, 0, 0, 0, 0);
        try
        {
            var output = Path.GetDirectoryName(TestPaths.AppExe)!;
            await using var backend = new MpvWorkerBackend(Path.Combine(output, "PUPlayer.MpvWorker.exe"), Path.Combine(output, "libmpv-2.dll"));
            await backend.LoadAsync(image, window, default);
            await Task.Delay(400);

            var frame = await backend.CaptureFrameAsync(default);

            Assert.True(frame.Width > 0);
        }
        finally { DestroyWindow(window); }
    }

    [Fact]
    public async Task WorkerBackend_SurvivesARejectedCapture()
    {
        var root = Environment.GetEnvironmentVariable("PUPLAYER_TEST_ROOT")!;
        var bad = Path.Combine(root, "not-media.txt");
        File.WriteAllText(bad, "not media");
        var window = CreateWindowEx(0, "STATIC", "", unchecked((int)0x80000000), 0, 0, 64, 64, 0, 0, 0, 0);
        try
        {
            var output = Path.GetDirectoryName(TestPaths.AppExe)!;
            await using var backend = new MpvWorkerBackend(Path.Combine(output, "PUPlayer.MpvWorker.exe"), Path.Combine(output, "libmpv-2.dll"));
            await backend.LoadAsync(bad, window, default);
            await Task.Delay(200);
            await Assert.ThrowsAnyAsync<Exception>(() => backend.CaptureFrameAsync(default));

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var snapshots = 0;
            await foreach (var _ in backend.Snapshots(timeout.Token))
                if (++snapshots == 2) break;
            Assert.Equal(2, snapshots);
        }
        finally { DestroyWindow(window); }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateWindowEx(int exStyle, string className, string windowName, int style,
        int x, int y, int width, int height, nint parent, nint menu, nint instance, nint parameter);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint window);
}
