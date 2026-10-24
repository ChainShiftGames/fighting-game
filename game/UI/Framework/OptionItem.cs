using System;
using System.Collections.Generic;

namespace FightingGame.UI.Framework;

/// <summary>A left-right picker between a few choices, e.g. "Vsync &lt; On &gt;"; confirm also steps forward.</summary>
/// <param name="Selected">Index of the choice shown now.</param>
/// <param name="Change">Called with the newly picked index.</param>
public sealed record OptionItem(
    MenuText Label, IReadOnlyList<MenuText> Choices, int Selected, Action<int> Change, bool Enabled = true)
    : MenuItem(Label, Enabled)
{
    public override bool Act(MenuAction actions)
    {
        MenuAction forward = MenuAction.Right | MenuAction.Confirm;
        int step = (actions & MenuAction.Left) != 0 ? -1 : (actions & forward) != 0 ? 1 : 0;
        if (step == 0 || Choices.Count == 0)
        {
            return false;
        }

        Change((Selected + step + Choices.Count) % Choices.Count);
        return true;
    }
}
