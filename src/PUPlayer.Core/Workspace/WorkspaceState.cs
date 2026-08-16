namespace PUPlayer.Core.Workspace;

public sealed record WorkspaceState(IReadOnlyList<MediaSlot> Slots, LayoutMode Layout)
{
    public static WorkspaceState Empty { get; } = new([], LayoutMode.Single);

    public WorkspaceState Add(string path) => Slots.Count >= 2
        ? throw new InvalidOperationException("A mosaic supports two videos.")
        : Normalize(Slots.Append(MediaSlot.Create(path)).ToArray(), Layout);

    public WorkspaceState Remove(Guid id) =>
        Normalize(Slots.Where(slot => slot.Id != id).ToArray(), Layout);

    public WorkspaceState Replace(Guid id, string path) =>
        Normalize(Slots.Select(slot => slot.Id == id ? MediaSlot.Create(path) : slot).ToArray(), Layout);

    public WorkspaceState ToggleLayout() => Slots.Count < 2 ? this : this with
    {
        Layout = Layout == LayoutMode.SplitHorizontal ? LayoutMode.SplitVertical : LayoutMode.SplitHorizontal
    };

    private static WorkspaceState Normalize(IReadOnlyList<MediaSlot> slots, LayoutMode layout) =>
        new(slots, slots.Count < 2 ? LayoutMode.Single : layout == LayoutMode.Single ? LayoutMode.SplitHorizontal : layout);
}
