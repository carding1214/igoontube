using System.Diagnostics;

namespace PUPlayer.Core.Playback;

public sealed class PlayerLoadMetrics(Func<long>? timestamp = null)
{
    private const long Unset = long.MinValue;
    private readonly Func<long> timestamp = timestamp ?? Stopwatch.GetTimestamp;
    private long windowVisible = Unset, workerReady = Unset, firstPlayableFrame = Unset;

    public long? WindowVisible => Read(windowVisible);
    public long? WorkerReady => Read(workerReady);
    public long? FirstPlayableFrame => Read(firstPlayableFrame);
    public void MarkWindowVisible() => Mark(ref windowVisible);
    public void MarkWorkerReady() => Mark(ref workerReady);
    public void MarkFirstPlayableFrame() => Mark(ref firstPlayableFrame);

    private void Mark(ref long value)
    {
        if (Volatile.Read(ref value) == Unset) Interlocked.CompareExchange(ref value, timestamp(), Unset);
    }

    private static long? Read(long value) => value == Unset ? null : value;
}
