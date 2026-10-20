namespace FightingGame.Presentation.World;

/// <summary>
/// Another model a character can be drawn with, playing the same clips: for comparing models side by side
/// (developer setting, switched in the debug panel).
/// </summary>
/// <param name="Model">The <c>.glb</c> (see <see cref="FighterArtFile.Resolve"/>).</param>
/// <param name="Scale">Multiplies the model's size, for a model not authored in metres.</param>
/// <param name="TurnDegrees">Turns the model about Y, for a model not facing +X.</param>
public sealed record FighterModelVariant(string Model, float Scale, float TurnDegrees);
