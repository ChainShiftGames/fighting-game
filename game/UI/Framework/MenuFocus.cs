using System.Collections.Generic;

namespace FightingGame.UI.Framework;

/// <summary>
/// Which item of one screen has the cursor. Kept by position, so items rebuilt with new labels ("Ready" →
/// "Ready — waiting") keep the cursor where it was.
/// </summary>
public sealed class MenuFocus
{
    /// <summary>The focused item's index; -1 while the screen has nothing to focus.</summary>
    public int Index { get; private set; } = -1;

    /// <summary>Puts the cursor back on a focusable item after the items changed: the nearest one, forward first.</summary>
    public int Settle(IReadOnlyList<MenuItem> items)
    {
        int start = Index < 0 ? 0 : Index >= items.Count ? items.Count - 1 : Index;
        Index = Nearest(items, start, 1);
        if (Index < 0)
        {
            Index = Nearest(items, start, -1);
        }

        return Index;
    }

    /// <summary>Moves to the next focusable item in <paramref name="step"/>'s direction; stays at the ends.</summary>
    public void Move(IReadOnlyList<MenuItem> items, int step)
    {
        Settle(items);
        if (Index < 0)
        {
            return;
        }

        int next = Nearest(items, Index + step, step);
        if (next >= 0)
        {
            Index = next;
        }
    }

    /// <summary>Focuses <paramref name="index"/> when it is focusable (e.g. under the mouse).</summary>
    public bool Set(IReadOnlyList<MenuItem> items, int index)
    {
        if (index < 0 || index >= items.Count || !items[index].IsFocusable)
        {
            return false;
        }

        Index = index;
        return true;
    }

    private static int Nearest(IReadOnlyList<MenuItem> items, int start, int step)
    {
        for (int i = start; i >= 0 && i < items.Count; i += step)
        {
            if (items[i].IsFocusable)
            {
                return i;
            }
        }

        return -1;
    }
}
