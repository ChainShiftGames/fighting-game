using System.Collections.Generic;

namespace FightingGame.UI.Framework;

/// <summary>One menu screen: what it shows, and what back and its other presses do. Kept on a <see cref="MenuStack"/>.</summary>
/// <remarks>Read every displayed frame, so its text and items may change while it is up (a lobby filling in).</remarks>
public interface IMenuScreen
{
    MenuText Title { get; }

    /// <summary>Lines of text above the items; may be empty.</summary>
    IReadOnlyList<MenuText> Body { get; }

    IReadOnlyList<MenuItem> Items { get; }

    /// <summary>The item the cursor starts on when the screen opens (e.g. the last mode played).</summary>
    int FirstFocus => 0;

    /// <summary>Shown over a match that stays visible behind it (pause, results), not as a full screen.</summary>
    bool IsOverMatch { get; }

    /// <summary>Back was pressed on this screen. True lets the stack pop it; the bottom screen is never popped.</summary>
    bool Back();

    /// <summary>A press neither the stack nor the focused item used (start, tabs…).</summary>
    void Press(MenuPress press);
}
