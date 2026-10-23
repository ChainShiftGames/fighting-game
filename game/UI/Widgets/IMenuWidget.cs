using System;
using FightingGame.UI.Framework;
using Godot;

namespace FightingGame.UI.Widgets;

/// <summary>Draws one kind of <see cref="MenuItem"/>, styled by the menu theme.</summary>
public interface IMenuWidget
{
    Control Control { get; }

    /// <summary>Whether it draws this kind of item, so a screen whose items changed reuses it.</summary>
    bool Draws(MenuItem item);

    void Show(MenuItem item, bool isFocused, MenuTextFormatter text);

    /// <summary>Reports the mouse moving over it and clicking it; widgets that can't be focused ignore this.</summary>
    void Listen(Action pointed, Action clicked);
}
