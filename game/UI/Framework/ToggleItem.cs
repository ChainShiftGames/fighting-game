using System;

namespace FightingGame.UI.Framework;

/// <summary>An on/off entry; confirm, left and right all flip it.</summary>
/// <param name="Change">Called with the new state.</param>
public sealed record ToggleItem(MenuText Label, bool IsOn, Action<bool> Change, bool Enabled = true)
    : MenuItem(Label, Enabled)
{
    public override bool Act(MenuAction actions)
    {
        if ((actions & (MenuAction.Confirm | MenuAction.Left | MenuAction.Right)) == 0)
        {
            return false;
        }

        Change(!IsOn);
        return true;
    }
}
