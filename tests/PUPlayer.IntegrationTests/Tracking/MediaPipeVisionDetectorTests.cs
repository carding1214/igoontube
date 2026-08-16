using PUPlayer.App.Tracking;
using System.IO;
using PUPlayer.Core.Playback;
using PUPlayer.IntegrationTests.App;

namespace PUPlayer.IntegrationTests.Tracking;

public sealed class MediaPipeVisionDetectorTests
{
    [Fact]
    public async Task LongLivedHost_ReturnsCorrelatedNormalizedLandmarks()
    {
        var python = Path.Combine(TestPaths.Repository, ".tools", "ai", ".venv", "Scripts", "python.exe");
        var host = Path.Combine(TestPaths.Repository, "tests", "fixtures", "fake_vision_host.py");
        await using var detector = new MediaPipeVisionDetector(python, host, "unused.task");

        var poses = await detector.DetectAsync(new VideoFrame(1, 1, [30, 20, 10]), default);

        Assert.Single(poses);
        Assert.Equal(.3, poses[0].Landmarks[0].X, 3);
        Assert.Equal(.9, poses[0].Landmarks[0].Visibility, 3);
    }

    [Fact]
    public async Task RealHost_AnalyzesAnRgbFrame()
    {
        var root = TestPaths.Repository;
        await using var detector = new MediaPipeVisionDetector(
            Path.Combine(root, ".tools", "ai", ".venv", "Scripts", "python.exe"),
            Path.Combine(root, "scripts", "vision_host.py"),
            Path.Combine(root, "data", "models", "mediapipe", "pose_landmarker_lite.task"));

        var poses = await detector.DetectAsync(new VideoFrame(384, 216, new byte[384 * 216 * 3]), default);

        Assert.Empty(poses);
    }
}
