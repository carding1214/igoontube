namespace PUPlayer.App.MediaTools;

public interface IThumbnailService
{
    Task<string> GetAsync(string source, double duration, double seconds, CancellationToken cancellationToken);
}
