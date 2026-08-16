namespace PUPlayer.Core.Playback;

public sealed record VideoFrame(int Width, int Height, byte[] Rgb24);
