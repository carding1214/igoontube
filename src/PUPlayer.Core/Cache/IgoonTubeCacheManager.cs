namespace PUPlayer.Core.Cache;

public sealed class IgoonTubeCacheManager : ICacheCatalog
{
    private readonly string globalRoot;
    private readonly Action<string> deleteFile;
    public IgoonTubeCacheManager(string globalRoot, Action<string>? deleteFile = null)
    {
        this.globalRoot = Path.GetFullPath(globalRoot);
        this.deleteFile = deleteFile ?? File.Delete;
    }

    public CacheReport ScanVideo(string mediaPath) => ScanAdjacent(Path.GetFullPath(mediaPath) + ".pucache");
    public CacheReport ScanGlobal() => ScanGlobalRoot();

    public CacheDeleteResult DeleteVideo(string mediaPath, CacheCategory categories) => Delete(ScanVideo(mediaPath), categories);
    public CacheDeleteResult DeleteGlobal(CacheCategory categories) => Delete(ScanGlobal(), categories);

    private static CacheReport ScanAdjacent(string root)
    {
        if (!Directory.Exists(root)) return new([]);
        var entries = new List<CacheEntry>();
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file);
            var name = Path.GetFileName(file);
            CacheCategory category;
            if (relative.StartsWith("thumbnails-v1" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                (name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".partial", StringComparison.OrdinalIgnoreCase)))
                category = CacheCategory.Thumbnails;
            else if ((name.StartsWith("voice-", StringComparison.OrdinalIgnoreCase) && (name.EndsWith(".mka", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))))
                category = CacheCategory.Audio;
            else if (name.StartsWith("scenes-", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                category = CacheCategory.Analysis;
            else if (relative.StartsWith("export-", StringComparison.OrdinalIgnoreCase) || name.StartsWith("igoontube-", StringComparison.OrdinalIgnoreCase) && name.Contains(".partial", StringComparison.OrdinalIgnoreCase))
                category = CacheCategory.Temporary;
            else continue;
            entries.Add(new(file, category, new FileInfo(file).Length));
        }
        return new(entries);
    }

    private CacheReport ScanGlobalRoot()
    {
        if (!Directory.Exists(globalRoot)) return new([]);
        var entries = Directory.EnumerateFiles(globalRoot, "*", SearchOption.AllDirectories)
            .Where(file => Path.GetFileName(file) is "voice.mka" or "manifest.json" || Path.GetFileName(file).Contains(".partial", StringComparison.OrdinalIgnoreCase))
            .Select(file => new CacheEntry(file, Path.GetFileName(file).Contains(".partial", StringComparison.OrdinalIgnoreCase) ? CacheCategory.Temporary : CacheCategory.Audio, new FileInfo(file).Length))
            .ToArray();
        return new(entries);
    }

    private CacheDeleteResult Delete(CacheReport report, CacheCategory categories)
    {
        long freed = 0;
        var deleted = 0;
        var failed = new List<string>();
        foreach (var entry in report.Entries.Where(x => categories.HasFlag(x.Category)))
        {
            try { deleteFile(entry.Path); freed += entry.Bytes; deleted++; }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { failed.Add(entry.Path); }
        }
        foreach (var directory in report.Entries.Select(x => Path.GetDirectoryName(x.Path)!).Distinct()
                     .OrderByDescending(x => x.Length))
            try { if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
        return new(freed, deleted, failed);
    }
}
