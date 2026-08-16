using System.Security.Cryptography;
using System.Text;

namespace PUPlayer.Core.AudioCache;

public sealed record AudioCacheKey(
    string Hash,
    string SourcePath,
    long SourceLength,
    DateTime SourceLastWriteUtc,
    string ModelId)
{
    public static AudioCacheKey FromFile(string sourcePath, string modelId)
    {
        var file = new FileInfo(sourcePath);
        if (!file.Exists) throw new FileNotFoundException("Source media not found.", sourcePath);
        return Create(file.FullName, file.Length, file.LastWriteTimeUtc, modelId);
    }

    public static AudioCacheKey Create(string sourcePath, long length, DateTime lastWriteUtc, string modelId)
    {
        if (!Path.IsPathFullyQualified(sourcePath)) throw new ArgumentException("Source path must be absolute.", nameof(sourcePath));
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        var fullPath = Path.GetFullPath(sourcePath);
        var utc = lastWriteUtc.ToUniversalTime();
        var payload = $"{fullPath.ToUpperInvariant()}\n{length}\n{utc.Ticks}\n{modelId}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))[..24].ToLowerInvariant();
        return new(hash, fullPath, length, utc, modelId);
    }
}
