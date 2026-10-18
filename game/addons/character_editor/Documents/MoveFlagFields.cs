using System.Collections.Generic;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>
/// The move fields that hold fighter flag names (ADR-0023). The move schema types them as plain ids because the names
/// live in <c>data/rules/flags.json</c>, so the editor offers that file's names as their choices.
/// </summary>
public static class MoveFlagFields
{
    /// <summary>The move schema with every flag list made a choice list of <paramref name="flagNames"/>.</summary>
    public static SchemaObject WithFlagChoices(SchemaObject moveSchema, IReadOnlyList<string> flagNames)
    {
        SchemaObject withRequires = moveSchema.Field(MoveFields.Requires) is { Object: { } requires } requiresField
            ? Replace(moveSchema, requiresField with
            {
                Object = OfferFlags(requires, [MoveFields.Flags, MoveFields.NotFlags], flagNames),
            })
            : moveSchema;
        return OfferFlags(withRequires, [MoveFields.SetsFlags, MoveFields.ClearsFlags], flagNames);
    }

    // Only plain lists: a schema that already lists the allowed names keeps its own choices.
    private static SchemaObject OfferFlags(SchemaObject schema, string[] names, IReadOnlyList<string> flagNames)
    {
        foreach (string name in names)
        {
            if (schema.Field(name) is { Kind: SchemaFieldKind.Other, Items: null } field)
            {
                schema = Replace(schema, field with { Kind = SchemaFieldKind.ChoiceList, Choices = flagNames });
            }
        }

        return schema;
    }

    private static SchemaObject Replace(SchemaObject schema, SchemaField replacement)
    {
        List<SchemaField> fields = [];
        foreach (SchemaField field in schema.Fields)
        {
            fields.Add(field.Name == replacement.Name ? replacement : field);
        }

        return new SchemaObject(fields, schema.Extra);
    }
}
