using System.Collections.Generic;
using FightingGame.Input.Profiles;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings.Controls;

/// <summary>
/// Menu text for inputs and controls. Directions and menu inputs have translation keys; buttons come from the button
/// set, so they show their id. Controls show their Godot name until button icons (UI-11).
/// </summary>
public static class InputNames
{
    public static MenuText Of(string input) => input switch
    {
        "up" => MenuText.Key("MENU_CONTROLS_INPUT_UP"),
        "down" => MenuText.Key("MENU_CONTROLS_INPUT_DOWN"),
        "left" => MenuText.Key("MENU_CONTROLS_INPUT_LEFT"),
        "right" => MenuText.Key("MENU_CONTROLS_INPUT_RIGHT"),
        MenuInputs.Confirm => MenuText.Key("MENU_CONTROLS_INPUT_CONFIRM"),
        MenuInputs.Back => MenuText.Key("MENU_CONTROLS_INPUT_BACK"),
        _ => MenuText.Key("MENU_CONTROLS_INPUT_BUTTON", char.ToUpperInvariant(input[0]) + input[1..]),
    };

    public static MenuText Of(BoundControl control) => MenuText.Plain(control.Name);

    /// <summary>The controls in a row, or "not bound".</summary>
    public static MenuText List(IReadOnlyList<BoundControl> controls)
    {
        if (controls.Count == 0)
        {
            return MenuText.Key("MENU_CONTROLS_UNBOUND");
        }

        var names = new string[controls.Count];
        for (int i = 0; i < names.Length; i++)
        {
            names[i] = controls[i].Name;
        }

        return MenuText.Plain(string.Join(", ", names));
    }

    /// <summary>The inputs, joined with the translated "and"-style separator.</summary>
    public static MenuText Join(IReadOnlyList<string> inputs)
    {
        MenuText joined = Of(inputs[0]);
        for (int i = 1; i < inputs.Count; i++)
        {
            joined = MenuText.Key("MENU_CONTROLS_INPUT_AND", joined, Of(inputs[i]));
        }

        return joined;
    }

    /// <summary>A warning for each control of <paramref name="input"/> that other inputs of its layer use too; may be empty.</summary>
    public static MenuText Shared(BindingProfile profile, BindingLayer layer, string input)
    {
        foreach (BoundControl control in profile.ControlsOf(layer, input))
        {
            List<string> others = profile.InputsBoundTo(layer, control, except: input);
            if (others.Count > 0)
            {
                return MenuText.Key("MENU_CONTROLS_SHARED", Of(control), Join(others));
            }
        }

        return MenuText.Empty;
    }
}
