namespace FightingGame.Presentation.World;

/// <summary>The stage art file: a picture behind the gameplay plane until stage art exists (developer setting).</summary>
/// <param name="Background">The image, relative to the stage art folder or a <c>res://</c> path; empty for none.</param>
/// <param name="GroundLine">Where the picture's ground meets the logic floor, as a share of its height from the bottom.</param>
/// <param name="Depth">How far behind the gameplay plane the picture stands, in metres.</param>
/// <param name="Parallax">How much of the camera's sideways travel the picture follows: 0 fixed to the stage, near 1
/// almost fixed to the screen. Following part of it, a smaller picture covers the whole stage and reads as far away.</param>
public sealed record StageArtSettings(string Background, float GroundLine, float Depth, float Parallax = 0f)
{
    public static StageArtSettings None { get; } = new(string.Empty, 0f, 0f);
}
