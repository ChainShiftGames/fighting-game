using System;
using Godot;

namespace FightingGame.VfxLab.Panels;

/// <summary>The drop-downs and toggles the Lab's bars are made of.</summary>
public static class LabChoice
{
    public static OptionButton Of(string tooltip, string[] items, int selected, Action<int> chosen)
    {
        OptionButton choice = new() { TooltipText = tooltip };
        foreach (string item in items)
        {
            choice.AddItem(item);
        }

        choice.Selected = selected;
        choice.ItemSelected += index => chosen((int)index);
        return choice;
    }

    public static CheckButton Toggle(string text, string tooltip, bool on, Action<bool> toggled)
    {
        CheckButton toggle = new() { Text = text, TooltipText = tooltip, ButtonPressed = on };
        toggle.Toggled += value => toggled(value);
        return toggle;
    }
}
