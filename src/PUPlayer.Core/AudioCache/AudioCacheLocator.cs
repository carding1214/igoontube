namespace PUPlayer.Core.AudioCache;

public sealed record AudioCacheLocation(string AudioPath, string ManifestPath, AudioCacheKey Key);

public sealed class AudioCacheLocator(string fallbackRoot)
{
    public AudioCacheLocation Locate(AudioCacheKey key)
    {
        if (!string.Equals(Path.GetPathRoot(key.SourcePath), @"C:\", StringComparison.OrdinalIgnoreCase))
        {
            var adjacent = key.SourcePath + ".pucache";
            try
            {
                Directory.CreateDirectory(adjacent);
                return new(
                    Path.Combine(adjacent, $"voice-{key.Hash}.mka"),
                    Path.Combine(adjacent, $"voice-{key.Hash}.json"), key);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }

        var directory = Path.Combine(Path.GetFullPath(fallbackRoot), key.Hash);
        Directory.CreateDirectory(directory);
        return new(Path.Combine(directory, "voice.mka"), Path.Combine(directory, "manifest.json"), key);
    }
}
