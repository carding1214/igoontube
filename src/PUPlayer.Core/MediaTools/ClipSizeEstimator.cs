namespace PUPlayer.Core.MediaTools;

public enum ClipEstimateMode { Original, CurrentView }

public static class ClipSizeEstimator
{
    public static long Estimate(long sourceBytes, double sourceDuration, double selectionDuration, ClipEstimateMode mode)
    {
        if (sourceBytes < 0) throw new ArgumentOutOfRangeException(nameof(sourceBytes));
        if (sourceDuration <= 0) throw new ArgumentOutOfRangeException(nameof(sourceDuration));
        if (selectionDuration <= 0) throw new ArgumentOutOfRangeException(nameof(selectionDuration));
        var bytes = mode == ClipEstimateMode.Original
            ? (decimal)sourceBytes * (decimal)(selectionDuration / sourceDuration)
            : (decimal)selectionDuration * 12_192_000m / 8m;
        return checked((long)Math.Ceiling(bytes * 1.15m));
    }
}
