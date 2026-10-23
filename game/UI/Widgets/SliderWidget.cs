using System.Globalization;
using FightingGame.UI.Framework;
using Godot;

namespace FightingGame.UI.Widgets;

/// <summary>Draws a <see cref="SliderItem"/>: its label, a bar and the number.</summary>
public partial class SliderWidget : MenuRow
{
    private const float BarWidth = 160f;

    private readonly ProgressBar _bar = new()
    {
        ShowPercentage = false,
        CustomMinimumSize = new Vector2(BarWidth, 0f),
        SizeFlagsVertical = SizeFlags.ShrinkCenter,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    private readonly Label _number = new() { ThemeTypeVariation = "MenuValue" };

    public SliderWidget()
    {
        Value.AddChild(_bar);
        Value.AddChild(_number);
    }

    public override bool Draws(MenuItem item) => item is SliderItem;

    protected override void ShowValue(MenuItem item, MenuTextFormatter text)
    {
        var slider = (SliderItem)item;
        _bar.MinValue = slider.Min;
        _bar.MaxValue = slider.Max;
        _bar.Value = slider.Value;
        _number.Text = slider.Value.ToString(CultureInfo.InvariantCulture);
    }
}
