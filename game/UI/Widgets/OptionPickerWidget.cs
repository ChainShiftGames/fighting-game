using FightingGame.UI.Framework;
using Godot;

namespace FightingGame.UI.Widgets;

/// <summary>Draws an <see cref="OptionItem"/>: "Label   &lt; Choice &gt;".</summary>
public partial class OptionPickerWidget : MenuRow
{
    private readonly Label _choice = new() { ThemeTypeVariation = "MenuValue" };

    public OptionPickerWidget() => Value.AddChild(_choice);

    public override bool Draws(MenuItem item) => item is OptionItem;

    protected override void ShowValue(MenuItem item, MenuTextFormatter text)
    {
        var option = (OptionItem)item;
        bool hasChoice = option.Selected >= 0 && option.Selected < option.Choices.Count;
        _choice.Text = hasChoice ? $"<  {text.Format(option.Choices[option.Selected])}  >" : string.Empty;
    }
}
