#if TOOLS
using System;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>One tab of the drawer, editing part of the selected move.</summary>
public interface IDrawerPage
{
    /// <summary>The page changed the move's file; <c>true</c> when frame ranges moved, so other pages show it again.</summary>
    event Action<bool>? Changed;

    /// <summary>Shows the move again from its file, e.g. when it is selected or another page changed it.</summary>
    void ShowMove(DrawerMove move);
}
#endif
