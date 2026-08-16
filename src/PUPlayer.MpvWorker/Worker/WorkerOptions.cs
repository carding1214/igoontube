using System.Globalization;

namespace PUPlayer.MpvWorker.Worker;

public sealed record WorkerOptions(string PipeName, string Token, ulong WindowId, string MpvPath)
{
    public static bool TryParse(string[] args, out WorkerOptions? options)
    {
        options = null;
        if (args.Length != 8) return false;
        var values = args.Chunk(2).ToDictionary(pair => pair[0], pair => pair[1], StringComparer.Ordinal);
        if (!values.TryGetValue("--pipe", out var pipe) || string.IsNullOrWhiteSpace(pipe) ||
            !values.TryGetValue("--token", out var token) || token.Length < 32 || !token.All(Uri.IsHexDigit) ||
            !values.TryGetValue("--wid", out var widText) || !ulong.TryParse(widText, NumberStyles.None, CultureInfo.InvariantCulture, out var wid) ||
            !values.TryGetValue("--mpv", out var mpv) || !Path.IsPathFullyQualified(mpv) || !File.Exists(mpv)) return false;
        options = new(pipe, token, wid, Path.GetFullPath(mpv));
        return true;
    }
}
