using System.Collections.Generic;

namespace FightingGame.UI.Framework;

/// <summary>
/// Turns menu presses into what they do on the top screen of a <see cref="MenuStack"/>: back, moving the cursor, the
/// focused item's own reaction, then the screen's; a screen that <see cref="IMenuScreen.TakesEveryPress"/> gets them
/// all. Nothing happens during the <see cref="MenuGrace"/>.
/// </summary>
public sealed class MenuNavigator
{
    private readonly MenuStack _stack;
    private readonly MenuGrace _grace;

    public MenuNavigator(MenuStack stack, MenuGrace grace)
    {
        _stack = stack;
        _grace = grace;
    }

    /// <summary>Acts on a press; false when no screen is open (so the press belongs to the match).</summary>
    public bool Handle(MenuPress press, long nowMilliseconds)
    {
        if (_stack.Top is not { } screen)
        {
            return false;
        }

        if (!_grace.HasPassed(nowMilliseconds))
        {
            return true;
        }

        if (screen.TakesEveryPress)
        {
            screen.Press(press);
            return true;
        }

        IReadOnlyList<MenuItem> items = screen.Items;
        MenuFocus focus = _stack.TopFocus!;
        if (press.Has(MenuAction.Back))
        {
            _stack.Back();
        }
        else if (press.Has(MenuAction.Up) || press.Has(MenuAction.Down))
        {
            focus.Move(items, press.Has(MenuAction.Up) ? -1 : 1);
        }
        else if (!ActOnFocused(items, focus, press.Actions))
        {
            screen.Press(press);
        }

        return true;
    }

    /// <summary>The mouse moved over an item: the cursor follows.</summary>
    public void Point(int index)
    {
        if (_stack.Top is { } screen)
        {
            _stack.TopFocus!.Set(screen.Items, index);
        }
    }

    /// <summary>An item was clicked: focus it and confirm, as a press would.</summary>
    public void Click(int index, long nowMilliseconds)
    {
        if (_stack.Top is { } screen && _stack.TopFocus!.Set(screen.Items, index))
        {
            Handle(new MenuPress(MenuAction.Confirm, MenuDevice.Mouse), nowMilliseconds);
        }
    }

    private static bool ActOnFocused(IReadOnlyList<MenuItem> items, MenuFocus focus, MenuAction actions)
    {
        int index = focus.Settle(items);
        return index >= 0 && items[index].Enabled && items[index].Act(actions);
    }
}
