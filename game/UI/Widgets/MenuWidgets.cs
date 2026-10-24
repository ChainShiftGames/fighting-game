using System;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Widgets;

/// <summary>Picks the widget for each kind of <see cref="MenuItem"/>: the one place a new kind is registered.</summary>
public static class MenuWidgets
{
    public static IMenuWidget For(MenuItem item) => item switch
    {
        ButtonItem => new ButtonWidget(),
        OptionItem => new OptionPickerWidget(),
        ToggleItem => new ToggleWidget(),
        SliderItem => new SliderWidget(),
        HeaderItem => new SectionHeaderWidget(),
        _ => throw new ArgumentException($"No widget draws {item.GetType().Name}.", nameof(item)),
    };
}
