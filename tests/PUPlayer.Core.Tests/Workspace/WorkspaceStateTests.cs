using PUPlayer.Core.Workspace;

namespace PUPlayer.Core.Tests.Workspace;

public sealed class WorkspaceStateTests
{
    [Fact]
    public void Add_AllowsTwoSlotsAndRejectsThird()
    {
        var state = WorkspaceState.Empty.Add(@"F:\media\one.mp4").Add(@"F:\media\two.mp4");

        Assert.Equal(2, state.Slots.Count);
        Assert.Throws<InvalidOperationException>(() => state.Add(@"F:\media\three.mp4"));
    }

    [Fact]
    public void ToggleLayout_SwitchesOnlySplitOrientations()
    {
        var state = WorkspaceState.Empty.Add("a").Add("b");

        Assert.Equal(LayoutMode.SplitVertical, state.ToggleLayout().Layout);
        Assert.Equal(LayoutMode.SplitHorizontal, state.ToggleLayout().ToggleLayout().Layout);
    }

    [Fact]
    public void Remove_ReturnsSingleLayout()
    {
        var state = WorkspaceState.Empty.Add("a").Add("b");

        var result = state.Remove(state.Slots[0].Id);

        Assert.Single(result.Slots);
        Assert.Equal(LayoutMode.Single, result.Layout);
    }

    [Fact]
    public void Replace_ChangesOnlySelectedSlot()
    {
        var state = WorkspaceState.Empty.Add("a").Add("b");

        var result = state.Replace(state.Slots[0].Id, "c");

        Assert.Equal(["c", "b"], result.Slots.Select(slot => slot.Path));
    }
}
