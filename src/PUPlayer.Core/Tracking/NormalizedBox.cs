namespace PUPlayer.Core.Tracking;

public sealed record NormalizedBox(double Left, double Top, double Right, double Bottom)
{
    public double Width => Right - Left;
    public double Height => Bottom - Top;
    public double CenterX => (Left + Right) / 2;
    public double CenterY => (Top + Bottom) / 2;

    public NormalizedBox Expand(double ratio) => new(
        Math.Clamp(Left - Width * ratio, 0, 1),
        Math.Clamp(Top - Height * ratio, 0, 1),
        Math.Clamp(Right + Width * ratio, 0, 1),
        Math.Clamp(Bottom + Height * ratio, 0, 1));

    public static NormalizedBox Lerp(NormalizedBox from, NormalizedBox to, double amount) => new(
        from.Left + (to.Left - from.Left) * amount,
        from.Top + (to.Top - from.Top) * amount,
        from.Right + (to.Right - from.Right) * amount,
        from.Bottom + (to.Bottom - from.Bottom) * amount);
}
