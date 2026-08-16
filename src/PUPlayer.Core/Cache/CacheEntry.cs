namespace PUPlayer.Core.Cache;

[Flags]
public enum CacheCategory { None = 0, Audio = 1, Thumbnails = 2, Analysis = 4, Temporary = 8, All = Audio | Thumbnails | Analysis | Temporary }
public sealed record CacheEntry(string Path, CacheCategory Category, long Bytes);
public sealed record CacheDeleteResult(long FreedBytes, int DeletedFiles, IReadOnlyList<string> FailedFiles);
public sealed record CacheReport(IReadOnlyList<CacheEntry> Entries)
{
    public long TotalBytes => Entries.Sum(x => x.Bytes);
    public long Bytes(CacheCategory category) => Entries.Where(x => category.HasFlag(x.Category)).Sum(x => x.Bytes);
}
