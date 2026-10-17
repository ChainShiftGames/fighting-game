using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// A move's <c>input</c>: its motion and the buttons pressed with it. One button is written as <c>button</c>, two or
/// more pressed together as <c>buttons</c>, the way the schema allows them.
/// </summary>
public static class MoveInput
{
    private static readonly string[] _fieldOrder = [MoveFields.Motion, MoveFields.Button, MoveFields.Buttons];

    /// <summary>The motion in numpad notation; "" when there is none.</summary>
    public static string Motion(JsonObject move) => JsonNodeValue.Text(move[MoveFields.Input]?[MoveFields.Motion]) ?? "";

    /// <summary>The button, or the buttons pressed together, in file order.</summary>
    public static IReadOnlyList<string> Buttons(JsonObject move)
    {
        if (move[MoveFields.Input] is not JsonObject input)
        {
            return [];
        }

        return JsonNodeValue.Text(input[MoveFields.Button]) is string button
            ? [button]
            : JsonStringList.Read(input, MoveFields.Buttons);
    }

    /// <param name="moveFieldOrder">The move's field names in schema order, for a move without input.</param>
    public static void SetMotion(JsonObject move, string motion, IReadOnlyList<string> moveFieldOrder) =>
        JsonFieldOrder.Set(Input(move, moveFieldOrder), MoveFields.Motion, motion, _fieldOrder);

    /// <summary>Writes the buttons; with none, both fields go and validation asks for one.</summary>
    /// <param name="moveFieldOrder">The move's field names in schema order, for a move without input.</param>
    public static void SetButtons(JsonObject move, IReadOnlyList<string> buttons, IReadOnlyList<string> moveFieldOrder)
    {
        JsonObject input = Input(move, moveFieldOrder);
        input.Remove(MoveFields.Button);
        input.Remove(MoveFields.Buttons);
        if (buttons.Count == 1)
        {
            JsonFieldOrder.Set(input, MoveFields.Button, buttons[0], _fieldOrder);
        }
        else if (buttons.Count > 1)
        {
            JsonArray list = [];
            foreach (string button in buttons)
            {
                list.Add(button);
            }

            JsonFieldOrder.Set(input, MoveFields.Buttons, list, _fieldOrder);
        }
    }

    private static JsonObject Input(JsonObject move, IReadOnlyList<string> moveFieldOrder)
    {
        if (move[MoveFields.Input] is not JsonObject input)
        {
            input = [];
            JsonFieldOrder.Set(move, MoveFields.Input, input, moveFieldOrder);
        }

        return input;
    }
}
