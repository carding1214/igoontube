namespace PUPlayer.Core.MediaTools;

public readonly record struct ClipSelection(double Start, double End)
{
    public double Duration => End - Start;

    public static ClipSelection FromMarks(double a, double b, double mediaDuration)
    {
        var value = new ClipSelection(
            Math.Clamp(Math.Min(a, b), 0, mediaDuration),
            Math.Clamp(Math.Max(a, b), 0, mediaDuration));
        return value.Duration >= .25 ? value : throw new ArgumentException("El clip debe durar al menos 0.25 segundos.");
    }
}
