namespace FightingGame.Online;

/// <summary>Which character one player plays, in which colour.</summary>
/// <param name="CharacterId">The character's id in the game data; null plays the default lineup's character.</param>
/// <param name="Colour">Which of the character's colours (its select file, ADR-0041); recorded in the replay header.</param>
public readonly record struct FighterPick(string? CharacterId, byte Colour)
{
    /// <summary>No pick yet (a launch from the command line): the default lineup's character in its first colour.</summary>
    public static FighterPick Default { get; } = new(null, 0);
}
