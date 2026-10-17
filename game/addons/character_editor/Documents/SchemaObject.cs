using System.Collections.Generic;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>The fields of one object in a data schema, in the order the schema lists them.</summary>
public sealed class SchemaObject
{
    public SchemaObject(IReadOnlyList<SchemaField> fields)
    {
        Fields = fields;
        List<string> names = [];
        foreach (SchemaField field in fields)
        {
            names.Add(field.Name);
        }

        FieldNames = names;
    }

    /// <summary>An object the schema says nothing about: the editor shows no fields for it.</summary>
    public static SchemaObject Empty { get; } = new([]);

    public IReadOnlyList<SchemaField> Fields { get; }

    /// <summary>The field names in schema order, the order saved files keep (<see cref="JsonFieldOrder"/>).</summary>
    public IReadOnlyList<string> FieldNames { get; }

    /// <summary>The field called <paramref name="name"/>; null when the schema has none.</summary>
    public SchemaField? Field(string name)
    {
        foreach (SchemaField field in Fields)
        {
            if (field.Name == name)
            {
                return field;
            }
        }

        return null;
    }

    /// <summary>The nested object's fields; <see cref="Empty"/> when the field isn't a known object.</summary>
    public SchemaObject ObjectOf(string name) => Field(name)?.Object ?? Empty;
}
