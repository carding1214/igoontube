namespace PUPlayer.Core.Workspace;

public sealed record MediaSlot(Guid Id, string Path)
{
    public static MediaSlot Create(string path) => new(Guid.NewGuid(), path);
}
