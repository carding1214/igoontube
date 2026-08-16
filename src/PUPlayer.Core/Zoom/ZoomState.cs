namespace PUPlayer.Core.Zoom;

public sealed record ZoomState(double Scale, double CenterX, double CenterY)
{
    public static ZoomState Default { get; } = new(1, .5, .5);

    public ZoomState ZoomAt(double factor, NormalizedPoint cursor)
    {
        var scale = Math.Clamp(Scale * factor, 1, 8);
        var x = CenterX + (cursor.X - .5) / Scale - (cursor.X - .5) / scale;
        var y = CenterY + (cursor.Y - .5) / Scale - (cursor.Y - .5) / scale;
        return Clamp(scale, x, y);
    }

    public ZoomState PanBy(double dx, double dy) =>
        Clamp(Scale, CenterX - dx / Scale, CenterY - dy / Scale);

    public MpvTransform ToMpv() => new(Math.Log2(Scale), .5 - CenterX, .5 - CenterY);

    private static ZoomState Clamp(double scale, double x, double y)
    {
        var edge = .5 / scale;
        return new(scale, Math.Clamp(x, edge, 1 - edge), Math.Clamp(y, edge, 1 - edge));
    }
}
