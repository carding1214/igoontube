using PUPlayer.Core.Zoom;

namespace PUPlayer.Core.Tracking;

public sealed class AutoFrameTracker
{
    private const double Visibility = .5;
    private NormalizedBox? _box;
    private (double X, double Y)? _selectedCenter;
    private TimeSpan? _lastSeen;
    private MpvTransform? _lastTransform;

    public SubjectRegion? CurrentRegion { get; private set; }
    public bool NeedsSelection { get; private set; }

    public void Select(IReadOnlyList<PoseCandidate> candidates, NormalizedPoint point)
    {
        var candidate = candidates.MinBy(item => Distance(item.CenterX, item.CenterY, point.X, point.Y));
        _selectedCenter = candidate is null ? null : (candidate.CenterX, candidate.CenterY);
        NeedsSelection = false;
        _box = null;
    }

    public MpvTransform? Update(IReadOnlyList<PoseCandidate> candidates, TimeSpan now)
    {
        var candidate = Choose(candidates);
        if (candidate is null && NeedsSelection) return null;
        if (candidate is null) return Missing(now);

        var framed = Frame(candidate);
        if (framed is null) return Missing(now);

        _selectedCenter = (candidate.CenterX, candidate.CenterY);
        _lastSeen = now;
        _box = _box is null ? framed.Value.Box : NormalizedBox.Lerp(_box, framed.Value.Box, .22);
        CurrentRegion = framed.Value.Region;
        _lastTransform = Transform(_box);
        return _lastTransform;
    }

    private PoseCandidate? Choose(IReadOnlyList<PoseCandidate> candidates)
    {
        if (candidates.Count == 0) return null;
        if (_selectedCenter is { } selected)
            return candidates.MinBy(item => Distance(item.CenterX, item.CenterY, selected.X, selected.Y));
        if (candidates.Count == 1) return candidates[0];
        NeedsSelection = true;
        return null;
    }

    private MpvTransform? Missing(TimeSpan now)
    {
        if (_lastSeen is not null && now - _lastSeen < TimeSpan.FromSeconds(2)) return _lastTransform;
        _box = null;
        _selectedCenter = null;
        _lastSeen = null;
        _lastTransform = new(0, 0, 0);
        CurrentRegion = null;
        NeedsSelection = false;
        return _lastTransform;
    }

    private static (NormalizedBox Box, SubjectRegion Region)? Frame(PoseCandidate candidate)
    {
        var points = candidate.Landmarks;
        if (Visible(points, 0, 11, 12, 23, 24, 27, 28))
            return (Bounds(points.Where(point => point.Visibility >= Visibility)).Expand(.15), SubjectRegion.FullBody);
        if (Visible(points, 11, 12, 23, 24))
            return (Bounds([points[11], points[12], points[23], points[24]]).Expand(.15), SubjectRegion.Torso);

        var face = points.Take(Math.Min(11, points.Count)).Where(point => point.Visibility >= Visibility).ToArray();
        return face.Length >= 3 && points.Count > 0 && points[0].Visibility >= Visibility
            ? (Bounds(face).Expand(.15), SubjectRegion.Face)
            : null;
    }

    private static bool Visible(IReadOnlyList<PoseLandmark> points, params int[] indices) =>
        indices.All(index => points.Count > index && points[index].Visibility >= Visibility);

    private static NormalizedBox Bounds(IEnumerable<PoseLandmark> values)
    {
        var points = values.ToArray();
        return new(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
    }

    private static MpvTransform Transform(NormalizedBox box)
    {
        var scale = Math.Clamp(1 / Math.Max(box.Width, box.Height), 1, 8);
        var edge = .5 / scale;
        var x = Math.Clamp(box.CenterX, edge, 1 - edge);
        var y = Math.Clamp(box.CenterY, edge, 1 - edge);
        return new(Math.Log2(scale), .5 - x, .5 - y);
    }

    private static double Distance(double x1, double y1, double x2, double y2) =>
        Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2);
}
