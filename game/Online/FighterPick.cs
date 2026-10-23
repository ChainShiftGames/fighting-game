namespace FightingGame.Online;

/// <summary>Which character one player plays, in which colour.</summary>
/// <param name="CharacterId">The character's id in the game data; null plays the default lineup's character.</param>
public readonly record struct FighterPick(string? CharacterId, byte Colour)
{
    /// <summary>Until character select (UI-05): the default lineup's character in its first colour.</summary>
    public static FighterPick Default { get; } = new(null, 0);
}
