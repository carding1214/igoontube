namespace PUPlayer.Core.Favorites;

public interface IFavoriteStore
{
    FavoriteIndex Load(string mediaPath);
    void Save(string mediaPath, IEnumerable<double> seconds);
}
