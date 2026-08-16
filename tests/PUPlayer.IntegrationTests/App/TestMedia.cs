using System.IO;

namespace PUPlayer.IntegrationTests.App;

internal static class TestMedia
{
    public static string OneSecondWave()
    {
        var root = Environment.GetEnvironmentVariable("PUPLAYER_TEST_ROOT")!;
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "one-second.wav");
        if (File.Exists(path)) return path;
        const int rate = 8000;
        const int dataSize = rate * 2;
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write("RIFF"u8); writer.Write(36 + dataSize); writer.Write("WAVE"u8);
        writer.Write("fmt "u8); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
        writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
        writer.Write("data"u8); writer.Write(dataSize); writer.Write(new byte[dataSize]);
        return path;
    }
}
