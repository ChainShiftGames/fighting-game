using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>A move's input as the player sees it: numpad motion as arrows (<c>236</c> → ↓↘→) and buttons as keycaps.</summary>
public static class MoveGlyphs
{
    // Numpad layout: 1 is down-back, 5 no direction, 9 up-forward (facing right).
    private static readonly string[] _arrows = ["", "↙", "↓", "↘", "←", "", "→", "↖", "↑", "↗"];

    /// <summary>The motion as arrows; no direction (5) draws nothing, so a standing normal is just its button.</summary>
    public static string Arrows(string motion)
    {
        var arrows = new StringBuilder();
        foreach (char step in motion)
        {
            if (step is >= '1' and <= '9')
            {
                arrows.Append(_arrows[step - '0']);
            }
        }

        return arrows.ToString();
    }

    /// <summary>The keycap labels: button ids in capitals.</summary>
    public static IReadOnlyList<string> Keycaps(IReadOnlyList<string> buttons)
    {
        List<string> caps = [];
        foreach (string button in buttons)
        {
            caps.Add(button.ToUpper(CultureInfo.InvariantCulture));
        }

        return caps;
    }
}
