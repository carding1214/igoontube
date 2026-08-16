using System.Globalization;
using System.Text.Json.Serialization;

namespace PUPlayer.Core.MediaTools;

public readonly record struct CropRect
{
    [JsonConstructor]
    public CropRect(double X, double Y, double Width, double Height)
    {
        if (X < 0 || Y < 0 || Width < .02 || Height < .02 || X + Width > 1 || Y + Height > 1)
            throw new ArgumentOutOfRangeException(nameof(Width), "El recorte debe estar dentro del video.");
        this.X = X; this.Y = Y; this.Width = Width; this.Height = Height;
    }

    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }
}

public sealed record VideoTransform
{
    public VideoTransform(int rotation = 0, bool mirrorX = false, bool mirrorY = false, CropRect? crop = null)
    {
        if (rotation is not (0 or 90 or 180 or 270)) throw new ArgumentOutOfRangeException(nameof(rotation));
        Rotation = rotation; MirrorX = mirrorX; MirrorY = mirrorY; Crop = crop;
    }

    public int Rotation { get; }
    public bool MirrorX { get; }
    public bool MirrorY { get; }
    public CropRect? Crop { get; }

    public string ToFfmpegFilter()
    {
        var filters = new List<string>();
        if (Crop is { } c)
        {
            static string N(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);
            filters.Add($"crop=iw*{N(c.Width)}:ih*{N(c.Height)}:iw*{N(c.X)}:ih*{N(c.Y)}");
        }
        if (MirrorX) filters.Add("hflip");
        if (MirrorY) filters.Add("vflip");
        if (Rotation == 90) filters.Add("transpose=1");
        else if (Rotation == 180) filters.Add("hflip,vflip");
        else if (Rotation == 270) filters.Add("transpose=2");
        return string.Join(',', filters);
    }
}
