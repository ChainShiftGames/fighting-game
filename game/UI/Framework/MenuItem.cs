namespace FightingGame.UI.Framework;

/// <summary>One line of a menu screen; each kind is drawn by its own widget (<c>Widgets/MenuWidgets</c>).</summary>
/// <param name="Enabled">Whether it can be used now; a disabled item can still hold focus (shown dimmed).</param>
public abstract record MenuItem(MenuText Label, bool Enabled = true)
{
    /// <summary>A short line under the label saying what it is; may be empty.</summary>
    public MenuText Description { get; init; } = MenuText.Empty;

    /// <summary>Whether the cursor can stop on it.</summary>
    public virtual bool IsFocusable => true;

    /// <summary>Reacts to a press while focused and enabled; false when the press means nothing to it.</summary>
    public abstract bool Act(MenuAction actions);
}
