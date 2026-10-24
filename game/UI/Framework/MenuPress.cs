namespace FightingGame.UI.Framework;

/// <summary>One menu press: what it means and which device made it.</summary>
/// <param name="IsRepeat">Made by holding a direction down, not by pressing it.</param>
public readonly record struct MenuPress(MenuAction Actions, MenuDevice Device, bool IsRepeat = false)
{
    public bool Has(MenuAction action) => (Actions & action) != 0;
}
