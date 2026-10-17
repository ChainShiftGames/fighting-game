namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>How the editor shows and edits a schema field.</summary>
public enum SchemaFieldKind
{
    /// <summary>Something the generic form doesn't edit (a list of objects, a fixed value): shown read-only.</summary>
    Other,
    Integer,
    Text,
    Boolean,

    /// <summary>One of the schema's listed strings.</summary>
    Choice,

    /// <summary>A list of distinct strings from the schema's list, such as a cancel's <c>on</c>.</summary>
    ChoiceList,

    /// <summary>An object with its own fields.</summary>
    Object,

    /// <summary>An object of free keys, each holding a value of one kind, such as a character's <c>moveRoles</c>.</summary>
    Map,
}
