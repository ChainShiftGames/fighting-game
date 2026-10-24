namespace FightingGame.UI.Framework;

/// <summary>A section heading between items; the cursor skips it.</summary>
public sealed record HeaderItem(MenuText Label) : MenuItem(Label)
{
    public override bool IsFocusable => false;

    public override bool Act(MenuAction actions) => false;
}
