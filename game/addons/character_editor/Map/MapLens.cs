namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>Which moves the move map shows.</summary>
public enum MapLens
{
    /// <summary>Attacks that combo: normals, specials and supers, plus jump cancels.</summary>
    Combos,

    /// <summary>Specials and supers only.</summary>
    SpecialMoves,

    /// <summary>Every move, including throws, movement and reactions.</summary>
    Everything,
}
