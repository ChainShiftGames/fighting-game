using System.Collections.Generic;
using Sim.Input;

namespace FightingGame.Input;

/// <summary>An input word as text for the developer overlay, e.g. <c>L R | a dash</c>.</summary>
public static class InputWordText
{
    /// <param name="buttons">Names the button bits; null shows directions only.</param>
    public static string Describe(InputWord word, ButtonSet? buttons)
    {
        var directions = new List<string>(4);
        AddIfHeld(directions, word, InputWord.Up, "U");
        AddIfHeld(directions, word, InputWord.Down, "D");
        AddIfHeld(directions, word, InputWord.Left, "L");
        AddIfHeld(directions, word, InputWord.Right, "R");

        var held = new List<string>();
        for (int button = 0; button < (buttons?.Count ?? 0); button++)
        {
            AddIfHeld(held, word, buttons!.MaskOf(button), buttons.IdOf(button));
        }

        string directionText = directions.Count == 0 ? "-" : string.Join(' ', directions);
        return held.Count == 0 ? directionText : $"{directionText} | {string.Join(' ', held)}";
    }

    private static void AddIfHeld(List<string> names, InputWord word, ushort mask, string name)
    {
        if (word.IsHeld(mask))
        {
            names.Add(name);
        }
    }
}
