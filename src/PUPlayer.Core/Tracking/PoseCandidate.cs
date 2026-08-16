namespace PUPlayer.Core.Tracking;

public sealed record PoseLandmark(double X, double Y, double Visibility);

public sealed record PoseCandidate(IReadOnlyList<PoseLandmark> Landmarks)
{
    public double CenterX => Visible().Average(point => point.X);
    public double CenterY => Visible().Average(point => point.Y);

    private IEnumerable<PoseLandmark> Visible() => Landmarks.Where(point => point.Visibility >= .5);
}

public enum SubjectRegion { FullBody, Torso, Face }
