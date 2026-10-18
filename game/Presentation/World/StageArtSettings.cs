namespace FightingGame.Presentation.World;

/// <summary>The stage art file: a built stage scene, or a picture behind the placeholder stage (developer setting).</summary>
/// <param name="Background">The image, relative to the stage art folder or a <c>res://</c> path; empty for none.</param>
/// <param name="GroundLine">Where the picture's ground meets the logic floor, as a share of its height from the bottom.</param>
/// <param name="Depth">How far behind the gameplay plane the picture stands, in metres.</param>
/// <param name="Parallax">How much of the camera's sideways travel the picture follows: 0 fixed to the stage, near 1
/// almost fixed to the screen. Following part of it, a smaller picture covers the whole stage and reads as far away.</param>
/// <param name="Floor">Keeps the stage's own floor in front of the picture, so the fighters stand on ground that moves
/// with them while a parallax picture is only distant scenery.</param>
/// <param name="Scene">A stage scene (<c>.tscn</c>) that replaces the placeholder stage whole: floor, walls, lights and
/// environment; relative to the stage art folder or a <c>res://</c> path; empty for none.</param>
/// <param name="Perspective">Draws the match through the narrow perspective camera, so a stage's depth shows
/// (ADR-0027 §4); off keeps the orthographic camera.</param>
public sealed record StageArtSettings(
    string Background,
    float GroundLine,
    float Depth,
    float Parallax = 0f,
    bool Floor = false,
    string Scene = "",
    bool Perspective = false)
{
    public static StageArtSettings None { get; } = new(string.Empty, 0f, 0f);
}
