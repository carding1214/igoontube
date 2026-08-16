using System.Text.Json;

namespace PUPlayer.Core.AudioCache;

public sealed record AudioCacheManifest(
    string SourcePath,
    long SourceLength,
    DateTime SourceLastWriteUtc,
    string ModelId,
    long AudioSize)
{
    public static AudioCacheManifest From(AudioCacheKey key, long audioSize) =>
        new(key.SourcePath, key.SourceLength, key.SourceLastWriteUtc, key.ModelId, audioSize);

    public bool Matches(AudioCacheKey key, string audioPath) =>
        AudioSize > 0 && File.Exists(audioPath) && new FileInfo(audioPath).Length == AudioSize &&
        string.Equals(SourcePath, key.SourcePath, StringComparison.OrdinalIgnoreCase) &&
        SourceLength == key.SourceLength && SourceLastWriteUtc == key.SourceLastWriteUtc &&
        string.Equals(ModelId, key.ModelId, StringComparison.Ordinal);

    public static AudioCacheManifest Load(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<AudioCacheManifest>(stream) ?? throw new InvalidDataException("Invalid audio cache manifest.");
    }

    public void SaveAtomic(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);
        var partial = path + ".partial-" + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(partial, JsonSerializer.Serialize(this));
            File.Move(partial, path, true);
        }
        finally
        {
            if (File.Exists(partial)) File.Delete(partial);
        }
    }
}
