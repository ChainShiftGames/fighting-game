using System.Collections.Generic;

namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>One field of an object in a data schema, as the editor needs it to build an input for it.</summary>
/// <param name="Name">The JSON key.</param>
/// <param name="Description">The schema's description; "" when it has none.</param>
/// <param name="Minimum">The smallest allowed whole number, for <see cref="SchemaFieldKind.Integer"/>.</param>
/// <param name="Maximum">The largest allowed whole number, for <see cref="SchemaFieldKind.Integer"/>.</param>
/// <param name="Choices">The allowed strings, for choices and choice lists; else empty.</param>
/// <param name="Object">The nested object's fields, for <see cref="SchemaFieldKind.Object"/>.</param>
/// <param name="Items">The fields of each item, for a list of objects (shown as <see cref="SchemaFieldKind.Other"/>).</param>
public sealed record SchemaField(
    string Name,
    SchemaFieldKind Kind,
    bool IsRequired,
    string Description,
    long? Minimum,
    long? Maximum,
    IReadOnlyList<string> Choices,
    SchemaObject? Object,
    SchemaObject? Items);
