using PUPlayer.Core.Tracking;
using PUPlayer.Core.Zoom;

namespace PUPlayer.Core.Tests.Tracking;

public sealed class AutoFrameTrackerTests
{
    [Fact]
    public void VisibleAnkles_SelectFullBody()
    {
        var tracker = new AutoFrameTracker();

        var transform = tracker.Update([Body(.5, ankles: true)], TimeSpan.Zero);

        Assert.Equal(SubjectRegion.FullBody, tracker.CurrentRegion);
        Assert.NotNull(transform);
        Assert.True(transform.VideoZoom > 0);
    }

    [Fact]
    public void MissingAnkles_FallsBackToTorso()
    {
        var tracker = new AutoFrameTracker();

        tracker.Update([Body(.5, ankles: false)], TimeSpan.Zero);

        Assert.Equal(SubjectRegion.Torso, tracker.CurrentRegion);
    }

    [Fact]
    public void FaceOnly_FallsBackToFace()
    {
        var tracker = new AutoFrameTracker();

        var transform = tracker.Update([Face(.5)], TimeSpan.Zero);

        Assert.Equal(SubjectRegion.Face, tracker.CurrentRegion);
        Assert.True(transform!.VideoZoom > 2);
    }

    [Fact]
    public void MultiplePeople_WaitsForNearestClickSelection()
    {
        var tracker = new AutoFrameTracker();
        var people = new[] { Body(.2, true), Body(.8, true) };

        Assert.Null(tracker.Update(people, TimeSpan.Zero));
        Assert.True(tracker.NeedsSelection);

        tracker.Select(people, new NormalizedPoint(.75, .5));
        var transform = tracker.Update(people, TimeSpan.FromMilliseconds(100));

        Assert.False(tracker.NeedsSelection);
        Assert.True(transform!.VideoPanX < 0);
    }

    [Fact]
    public void Movement_IsSmoothedInsteadOfJumping()
    {
        var tracker = new AutoFrameTracker();
        tracker.Update([Body(.25, true)], TimeSpan.Zero);

        var transform = tracker.Update([Body(.75, true)], TimeSpan.FromMilliseconds(200));

        Assert.InRange(transform!.VideoPanX, 0.05, 0.20);
    }

    [Fact]
    public void LostSubject_HoldsThenResetsAfterTwoSeconds()
    {
        var tracker = new AutoFrameTracker();
        var tracked = tracker.Update([Body(.3, true)], TimeSpan.Zero);

        Assert.Equal(tracked, tracker.Update([], TimeSpan.FromSeconds(1.9)));
        Assert.Equal(new MpvTransform(0, 0, 0), tracker.Update([], TimeSpan.FromSeconds(2.1)));
        Assert.Null(tracker.CurrentRegion);
    }

    private static PoseCandidate Body(double centerX, bool ankles)
    {
        var points = Invisible();
        Set(points, 0, centerX, .2);
        Set(points, 11, centerX - .1, .35); Set(points, 12, centerX + .1, .35);
        Set(points, 23, centerX - .08, .58); Set(points, 24, centerX + .08, .58);
        if (ankles) { Set(points, 27, centerX - .06, .8); Set(points, 28, centerX + .06, .8); }
        return new(points);
    }

    private static PoseCandidate Face(double centerX)
    {
        var points = Invisible();
        for (var i = 0; i <= 10; i++) Set(points, i, centerX + (i % 2 == 0 ? -.03 : .03), .25 + i % 3 * .02);
        return new(points);
    }

    private static PoseLandmark[] Invisible() =>
        Enumerable.Repeat(new PoseLandmark(0, 0, 0), 33).ToArray();

    private static void Set(PoseLandmark[] points, int index, double x, double y) =>
        points[index] = new(x, y, 1);
}
