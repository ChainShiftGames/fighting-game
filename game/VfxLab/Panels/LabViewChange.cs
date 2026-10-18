namespace FightingGame.VfxLab.Panels;

/// <summary>What a change to the Lab's view options needs redone.</summary>
public enum LabViewChange
{
    /// <summary>Which variants show or how they're laid out: the grid is rebuilt.</summary>
    Grid,

    /// <summary>The values variants are drawn with: every view applies them again.</summary>
    Values,

    /// <summary>Only how the views draw the frame; the next frame picks it up.</summary>
    Look,
}
