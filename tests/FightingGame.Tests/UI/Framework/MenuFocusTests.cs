using System.Collections.Generic;
using FightingGame.UI.Framework;
using Xunit;

namespace FightingGame.Tests.UI.Framework;

public sealed class MenuFocusTests
{
    private static readonly MenuText _label = MenuText.Plain("Item");

    [Fact]
    public void StartsOnTheFirstFocusableItemSkippingHeaders()
    {
        var focus = new MenuFocus();

        Assert.Equal(1, focus.Settle([Header(), Button(), Button()]));
    }

    [Fact]
    public void MovingSkipsHeadersAndStopsAtTheEnds()
    {
        List<MenuItem> items = [Button(), Header(), Button()];
        var focus = new MenuFocus();

        focus.Move(items, 1);
        Assert.Equal(2, focus.Index);
        focus.Move(items, 1);
        Assert.Equal(2, focus.Index);
        focus.Move(items, -1);
        focus.Move(items, -1);
        Assert.Equal(0, focus.Index);
    }

    [Fact]
    public void DisabledItemsCanHoldFocus()
    {
        List<MenuItem> items = [Button(), Button(enabled: false)];
        var focus = new MenuFocus();

        focus.Move(items, 1);

        Assert.Equal(1, focus.Index);
    }

    [Fact]
    public void ItemsRebuiltWithNewLabelsKeepTheFocusedPosition()
    {
        var focus = new MenuFocus();
        focus.Move([Button(), Button()], 1);

        // "Ready" becomes "Ready — waiting": same position, new item.
        int index = focus.Settle([Button(), new ButtonItem(MenuText.Plain("Ready — waiting"), () => { }, Enabled: false)]);

        Assert.Equal(1, index);
    }

    [Fact]
    public void FewerItemsMoveTheFocusToTheLastOne()
    {
        var focus = new MenuFocus();
        List<MenuItem> many = [Button(), Button(), Button(), Button()];
        focus.Move(many, 1);
        focus.Move(many, 1);
        focus.Move(many, 1);

        Assert.Equal(1, focus.Settle([Button(), Button()]));
    }

    [Fact]
    public void NothingToFocusIsMinusOne()
    {
        var focus = new MenuFocus();

        Assert.Equal(-1, focus.Settle([Header()]));
        Assert.Equal(-1, focus.Settle([]));
    }

    [Fact]
    public void SetIgnoresHeadersAndOutOfRangeIndexes()
    {
        List<MenuItem> items = [Button(), Header()];
        var focus = new MenuFocus();

        Assert.False(focus.Set(items, 1));
        Assert.False(focus.Set(items, 5));
        Assert.True(focus.Set(items, 0));
    }

    private static ButtonItem Button(bool enabled = true) => new(_label, () => { }, enabled);

    private static HeaderItem Header() => new(_label);
}
