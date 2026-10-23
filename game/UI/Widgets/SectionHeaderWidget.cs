using System;
using FightingGame.UI.Framework;
using Godot;

namespace FightingGame.UI.Widgets;

/// <summary>Draws a <see cref="HeaderItem"/>: a heading between items, never focused.</summary>
public partial class SectionHeaderWidget : Label, IMenuWidget
{
    public SectionHeaderWidget() => ThemeTypeVariation = "MenuHeader";

    public Control Control => this;

    public bool Draws(MenuItem item) => item is HeaderItem;

    public void Show(MenuItem item, bool isFocused, MenuTextFormatter text) => Text = text.Format(item.Label);

    public void Listen(Action pointed, Action clicked)
    {
    }
}
