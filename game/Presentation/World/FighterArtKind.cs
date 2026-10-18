namespace FightingGame.Presentation.World;

/// <summary>Which <see cref="IFighterArt"/> a character is drawn with (developer setting, ADR-0028).</summary>
public enum FighterArtKind
{
    /// <summary>The box-shaped <see cref="PlaceholderFighterModel"/>, sized by the body box.</summary>
    Placeholder,

    /// <summary>The character's rigged model, <see cref="RiggedFighterModel"/>.</summary>
    Model,
}
