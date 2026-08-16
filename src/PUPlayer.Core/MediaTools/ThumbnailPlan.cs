namespace PUPlayer.Core.MediaTools;

public readonly record struct ThumbnailPlan(int Count, double Interval)
{
    public static ThumbnailPlan ForDuration(double duration)
    {
        var count = Math.Clamp((int)Math.Round(Math.Max(0, duration) / 40), 60, 300);
        return new(count, duration > 0 ? duration / count : 0);
    }
}
