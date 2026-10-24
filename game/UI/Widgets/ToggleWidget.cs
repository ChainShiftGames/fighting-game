using FightingGame.UI.Framework;
using Godot;

namespace FightingGame.UI.Widgets;

/// <summary>Draws a <see cref="ToggleItem"/>: its label and On or Off.</summary>
public partial class ToggleWidget : MenuRow
{
    private static readonly MenuText _on = MenuText.Key("MENU_ON");
    private static readonly MenuText _off = MenuText.Key("MENU_OFF");

    private readonly Label _state = new() { ThemeTypeVariation = "MenuValue" };

    public ToggleWidget() => Value.AddChild(_state);

    public override bool Draws(MenuItem item) => item is ToggleItem;

    protected override void ShowValue(MenuItem item, MenuTextFormatter text) =>
        _state.Text = text.Format(((ToggleItem)item).IsOn ? _on : _off);
}
