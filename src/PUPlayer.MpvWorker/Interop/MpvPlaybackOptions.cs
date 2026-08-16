namespace PUPlayer.MpvWorker.Interop;

public static class MpvPlaybackOptions
{
    public static IReadOnlyDictionary<string, string> Values { get; } = new Dictionary<string, string>
    {
        ["vo"] = "gpu-next", ["hwdec"] = "auto-safe", ["volume-max"] = "200", ["config"] = "no",
        ["osc"] = "no", ["input-default-bindings"] = "no", ["input-vo-keyboard"] = "no", ["terminal"] = "no",
        ["save-position-on-quit"] = "no", ["idle"] = "yes", ["demuxer-thread"] = "yes", ["cache"] = "yes",
        ["cache-secs"] = "5", ["demuxer-readahead-secs"] = "5", ["demuxer-max-bytes"] = "64MiB",
        ["demuxer-max-back-bytes"] = "16MiB", ["cache-pause-initial"] = "no"
    };
}
