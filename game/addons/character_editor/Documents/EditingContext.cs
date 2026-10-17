using System.Collections.Generic;
using Sim.Data.Input;
using Sim.Data.Rules;
using Sim.Data.Sources;
using Sim.Data.Validation;
using Sim.Input;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// What the editor's forms need besides the character itself: the move and character file schemas, the button
/// set's button ids and the fighter flag names. Read once per opened character, so schema edits show up on Reload.
/// </summary>
/// <param name="MoveSchema">
/// The move file's fields, its flag lists offering the names in <c>rules/flags.json</c> (<see cref="MoveFlagFields"/>);
/// empty when <c>data/schema/</c> has no readable move schema.
/// </param>
/// <param name="CharacterSchema">The character file's fields; empty likewise.</param>
/// <param name="ButtonIds">The button set's buttons in UI order; empty when it can't be read (validation reports it).</param>
public sealed record EditingContext(
    SchemaObject MoveSchema, SchemaObject CharacterSchema, IReadOnlyList<string> ButtonIds)
{
    public static EditingContext Read(IDataSource source)
    {
        ButtonSet? buttons = ButtonSetLoader.Load(source, new DataErrors());
        // Unreadable flags offer no names; validation reports the file.
        IReadOnlyList<string> flagNames = FighterFlagsLoader.Load(source, new DataErrors())?.Names.Names ?? [];
        SchemaObject moveSchema = DataSchema.Read(source, "move")?.Root ?? SchemaObject.Empty;
        return new EditingContext(
            MoveFlagFields.WithFlagChoices(moveSchema, flagNames),
            DataSchema.Read(source, "character")?.Root ?? SchemaObject.Empty,
            buttons is null ? [] : new ButtonSetLookup(buttons).ButtonIds);
    }

    /// <summary>The first button, which a new attack starts on; "" without a button set.</summary>
    public string FirstButton => ButtonIds.Count > 0 ? ButtonIds[0] : "";
}
