using System.Collections.Generic;
using Sim.Data.Input;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Input;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// What the editor's forms need besides the character itself: the move and character file schemas and the button
/// set's button ids. Read once per opened character, so schema edits show up on Reload.
/// </summary>
/// <param name="MoveSchema">The move file's fields; empty when <c>data/schema/</c> has no readable move schema.</param>
/// <param name="CharacterSchema">The character file's fields; empty likewise.</param>
/// <param name="ButtonIds">The button set's buttons in UI order; empty when it can't be read (validation reports it).</param>
public sealed record EditingContext(
    SchemaObject MoveSchema, SchemaObject CharacterSchema, IReadOnlyList<string> ButtonIds)
{
    public static EditingContext Read(IDataSource source)
    {
        ButtonSet? buttons = ButtonSetLoader.Load(source, new DataErrors());
        return new EditingContext(
            DataSchema.Read(source, "move")?.Root ?? SchemaObject.Empty,
            DataSchema.Read(source, "character")?.Root ?? SchemaObject.Empty,
            buttons is null ? [] : new ButtonSetLookup(buttons).ButtonIds);
    }

    /// <summary>The first button, which a new attack starts on; "" without a button set.</summary>
    public string FirstButton => ButtonIds.Count > 0 ? ButtonIds[0] : "";
}
