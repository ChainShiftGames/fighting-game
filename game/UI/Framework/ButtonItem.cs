using System;

namespace FightingGame.UI.Framework;

/// <summary>A list entry that does something when confirmed.</summary>
public sealed record ButtonItem(MenuText Label, Action Choose, bool Enabled = true) : MenuItem(Label, Enabled)
{
    public override bool Act(MenuAction actions)
    {
        if ((actions & MenuAction.Confirm) == 0)
        {
            return false;
        }

        Choose();
        return true;
    }
}
