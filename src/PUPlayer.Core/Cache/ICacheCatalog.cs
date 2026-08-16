namespace PUPlayer.Core.Cache;

public interface ICacheCatalog
{
    CacheReport ScanVideo(string mediaPath);
    CacheReport ScanGlobal();
    CacheDeleteResult DeleteVideo(string mediaPath, CacheCategory categories);
    CacheDeleteResult DeleteGlobal(CacheCategory categories);
}
