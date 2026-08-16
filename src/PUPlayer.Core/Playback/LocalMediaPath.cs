namespace PUPlayer.Core.Playback;

public readonly record struct LocalMediaPath(string Value)
{
    public static bool TryCreate(string value, out LocalMediaPath path)
    {
        path = default;
        if (!Path.IsPathFullyQualified(value) || !File.Exists(value)) return false;
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !uri.IsFile) return false;
        path = new(Path.GetFullPath(value));
        return true;
    }
}
