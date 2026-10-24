using System;

namespace FightingGame.UI.Framework;

/// <summary>A number between <paramref name="Min"/> and <paramref name="Max"/>, moved by left and right.</summary>
/// <param name="Change">Called with the new value, always within the range.</param>
public sealed record SliderItem(
    MenuText Label, int Value, int Min, int Max, int Step, Action<int> Change, bool Enabled = true)
    : MenuItem(Label, Enabled)
{
    public override bool Act(MenuAction actions)
    {
        int step = (actions & MenuAction.Left) != 0 ? -Step : (actions & MenuAction.Right) != 0 ? Step : 0;
        if (step == 0)
        {
            return false;
        }

        int value = Math.Clamp(Value + step, Min, Max);
        if (value != Value)
        {
            Change(value);
        }

        return true;
    }
}
