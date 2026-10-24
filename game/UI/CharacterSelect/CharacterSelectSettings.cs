namespace FightingGame.UI.CharacterSelect;

/// <summary>How character select is laid out and paced.</summary>
/// <remarks>Developer settings (ARCHITECTURE.md §6); <see cref="Default"/> is the one place they are set.</remarks>
/// <param name="Columns">Portraits per row of the grid; the random slot comes after the last character.</param>
/// <param name="BothReadyMilliseconds">How long "both ready" shows before stage select, so a last back can still undo it.</param>
public sealed record CharacterSelectSettings(int Columns, long BothReadyMilliseconds)
{
    public static CharacterSelectSettings Default { get; } = new(Columns: 6, BothReadyMilliseconds: 900);
}
