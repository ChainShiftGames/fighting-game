using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>
/// Plain names for moves, so someone who doesn't know numpad notation can read the map: <c>5b</c> is Standing
/// Medium, <c>236c</c> a Quarter-circle special. Ids that aren't notation (<c>forward_throw</c>) read as words.
/// </summary>
public static class MoveNames
{
    private const string SpecialFallback = "Special move";
    private const string SuperName = "Super";
    private const string AirPrefix = "j_";
    private const string Neutral = "5";

    public static string Of(string id, JsonObject move, MoveMapSettings settings)
    {
        string category = JsonNodeValue.Text(move[MoveFields.Category]) ?? "";
        string motion = MoveInput.Motion(move);
        IReadOnlyList<string> buttons = MoveInput.Buttons(move);
        bool inAir = JsonNodeValue.Text(move[MoveFields.UsableIn]) == "air";
        if (buttons.Count == 0 || id != Notation(motion, buttons, inAir))
        {
            return Words(id);
        }

        return category switch
        {
            "normal" => $"{Stance(motion, inAir, settings)} {Strength(buttons, settings)}",
            "special" => settings.SpecialNames.GetValueOrDefault(motion, SpecialFallback),
            "super" => SuperName,
            _ => Words(id),
        };
    }

    /// <summary>The id written as words: <c>walk_forward</c> → Walk forward.</summary>
    public static string Words(string id)
    {
        string spaced = id.Replace('_', ' ');
        return spaced.Length == 0 ? spaced : char.ToUpperInvariant(spaced[0]) + spaced[1..];
    }

    // The id a move gets in numpad notation: 5b, 236236d, j_c (an air normal with no direction), j_2c.
    private static string Notation(string motion, IReadOnlyList<string> buttons, bool inAir)
    {
        var id = new StringBuilder(inAir ? AirPrefix : "");
        id.Append(inAir && motion == Neutral ? "" : motion);
        foreach (string button in buttons)
        {
            id.Append(button);
        }

        return id.ToString();
    }

    private static string Stance(string motion, bool inAir, MoveMapSettings settings) =>
        settings.StanceNames.GetValueOrDefault(inAir ? MoveMapSettings.AirStance : motion, motion);

    private static string Strength(IReadOnlyList<string> buttons, MoveMapSettings settings)
    {
        List<string> names = [];
        foreach (string button in buttons)
        {
            names.Add(settings.ButtonNames.GetValueOrDefault(button, button.ToUpper(CultureInfo.InvariantCulture)));
        }

        return string.Join(" + ", names);
    }
}
