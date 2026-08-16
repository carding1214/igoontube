namespace PUPlayer.Core.MediaTools;

public static class ClipOutputNamer
{
    public static string Next(string sourcePath, Func<string, bool>? exists = null)
    {
        exists ??= File.Exists;
        var directory = Path.GetDirectoryName(sourcePath) ?? throw new ArgumentException("Ruta sin carpeta.", nameof(sourcePath));
        var name = Path.GetFileNameWithoutExtension(sourcePath);
        for (var index = 1; index < 10_000; index++)
        {
            var candidate = Path.Combine(directory, $"{name}_clip_{index:000}.mp4");
            if (!exists(candidate)) return candidate;
        }
        throw new IOException("No hay un nombre de clip disponible.");
    }
}
