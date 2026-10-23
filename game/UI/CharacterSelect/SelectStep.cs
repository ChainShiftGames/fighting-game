namespace FightingGame.UI.CharacterSelect;

/// <summary>How far one player is on character select.</summary>
public enum SelectStep
{
    /// <summary>Moving the cursor over the grid.</summary>
    Character,

    /// <summary>A character is chosen; left and right cycle its colours.</summary>
    Colour,

    /// <summary>Character and colour are locked in.</summary>
    Ready,
}
