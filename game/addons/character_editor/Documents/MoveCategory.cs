using System.Text.Json.Nodes;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// Changes a move's category. Between attack categories only the field changes; between basic and attack the fields
/// that come with it change too (<c>duration</c> ⇄ <c>frameData</c>, <c>input</c>, <c>usableIn</c>), keeping the
/// move's length so its boxes stay inside it. Anything else the new category doesn't allow, such as hit boxes on a
/// basic move, is left for validation to report rather than deleted.
/// </summary>
public static class MoveCategory
{
    /// <summary>Numpad notation for no direction: a new attack is a standing normal until the user sets its motion.</summary>
    public const string NeutralMotion = "5";

    /// <param name="moveSchema">The move file's schema: field order and the first <c>usableIn</c> choice.</param>
    /// <param name="firstButton">The button set's first button, for an attack made from a basic move.</param>
    public static void Set(JsonObject move, string category, SchemaObject moveSchema, string firstButton)
    {
        bool wasBasic = JsonNodeValue.Text(move[MoveFields.Category]) == MoveFields.BasicCategory;
        bool isBasic = category == MoveFields.BasicCategory;
        JsonFieldOrder.Set(move, MoveFields.Category, category, moveSchema.FieldNames);
        if (wasBasic == isBasic)
        {
            return;
        }

        int frames = FramePhases.Of(move)?.TotalFrames ?? 1;
        if (isBasic)
        {
            move.Remove(MoveFields.FrameData);
            move.Remove(MoveFields.Input);
            move.Remove(MoveFields.UsableIn);
            MoveFrameData.Set(move, FramePhases.Basic(frames), moveRanges: false, moveSchema.FieldNames);
            return;
        }

        move.Remove(MoveFields.Duration);
        // Startup 1 and active 1 are the shortest attack; recovery takes the rest, so the length stays the same.
        MoveFrameData.Set(move, FramePhases.Attack(1, 1, frames - 1), moveRanges: false, moveSchema.FieldNames);
        if (moveSchema.Field(MoveFields.UsableIn) is SchemaField usableIn)
        {
            JsonFieldOrder.Set(move, MoveFields.UsableIn, SchemaDefaults.ValueFor(usableIn), moveSchema.FieldNames);
        }

        MoveInput.SetMotion(move, NeutralMotion, moveSchema.FieldNames);
        MoveInput.SetButtons(move, [firstButton], moveSchema.FieldNames);
    }
}
