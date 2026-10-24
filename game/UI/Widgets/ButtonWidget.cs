using FightingGame.UI.Framework;

namespace FightingGame.UI.Widgets;

/// <summary>Draws a <see cref="ButtonItem"/>: just its label.</summary>
public partial class ButtonWidget : MenuRow
{
    public override bool Draws(MenuItem item) => item is ButtonItem;

    protected override void ShowValue(MenuItem item, MenuTextFormatter text)
    {
    }
}
