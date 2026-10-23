namespace FightingGame.UI.CharacterSelect;

/// <summary>One player's place on character select: cursor, step and what they chose.</summary>
public sealed class SideSelection
{
    /// <summary>The grid slot the cursor is on.</summary>
    public int Slot { get; internal set; }

    public SelectStep Step { get; internal set; }

    /// <summary>The chosen character's index in the grid; only meaningful past <see cref="SelectStep.Character"/>.</summary>
    public int Character { get; internal set; }

    /// <summary>The chosen colour of <see cref="Character"/>.</summary>
    public int Colour { get; internal set; }

    /// <summary>Chosen through the random slot, so back returns to the grid, not to the colour.</summary>
    public bool IsRandom { get; internal set; }

    /// <summary>The character and colour last played this session, where the cursor and colour start; -1 for none.</summary>
    internal int RememberedCharacter { get; set; } = -1;

    internal int RememberedColour { get; set; }
}
