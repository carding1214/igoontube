namespace PUPlayer.App.Features;

public static class BuildCapabilities
{
#if IGOONTUBE_NO_AI
    public const bool AiAvailable = false;
#else
    public const bool AiAvailable = true;
#endif
}
