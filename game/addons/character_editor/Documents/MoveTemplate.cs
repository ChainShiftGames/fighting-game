using System.Text.Json.Nodes;
using Sim.Data.Characters.Reading;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>The JSON of a move the user creates in the editor: a new, empty one or a copy of another.</summary>
public static class MoveTemplate
{
    /// <summary>Where a move file's <c>$schema</c> points, relative to <c>characters/&lt;id&gt;/moves/</c>.</summary>
    public const string SchemaReference = "../../../schema/move.schema.json";

    /// <summary>
    /// The shortest move of the category, with no boxes: a 1-frame basic move, or a standing attack on the first
    /// button (see <see cref="MoveCategory"/>). The user then sets its frames and boxes.
    /// </summary>
    public static JsonObject New(string moveId, string category, SchemaObject moveSchema, string firstButton)
    {
        JsonObject move = new()
        {
            [MoveFields.Schema] = SchemaReference,
            [MoveFields.SchemaVersion] = CharacterSchema.MoveVersion,
            [MoveFields.Id] = moveId,
            [MoveFields.Category] = MoveFields.BasicCategory,
            [MoveFields.Duration] = 1,
            [MoveFields.Boxes] = new JsonArray(),
        };

        MoveCategory.Set(move, category, moveSchema, firstButton);
        return move;
    }

    /// <summary>A deep copy of <paramref name="source"/> under the new id; the source is left unchanged.</summary>
    public static JsonObject Copy(JsonObject source, string moveId, SchemaObject moveSchema)
    {
        JsonObject copy = source.DeepClone().AsObject();
        JsonFieldOrder.Set(copy, MoveFields.Id, moveId, moveSchema.FieldNames);
        return copy;
    }
}
