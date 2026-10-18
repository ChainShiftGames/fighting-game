using FightingGame.Presentation.World;
using Godot;

namespace FightingGame.VfxLab.View;

/// <summary>The stage art file's picture for the Lab's backdrop, standing as the match's stage does.</summary>
/// <param name="Rect">Left, bottom, width and height in metres.</param>
public sealed record LabStagePicture(Texture2D Texture, Vector4 Rect)
{
    // Developer settings (presentation): how wide the picture is drawn and where its left edge is, in metres, so it
    // spans a cast that starts at the caster (x = 0).
    private const float Width = 16f;
    private const float Left = -5f;

    /// <summary>The picture, or null when there is no stage art file or picture.</summary>
    public static LabStagePicture? Load()
    {
        if (!FileAccess.FileExists(StageArtFile.Path) ||
            StageArtFile.Read(FileAccess.GetFileAsString(StageArtFile.Path), out _) is not { Background.Length: > 0 } art)
        {
            return null;
        }

        string path = StageArtFile.Resolve(art.Background);
        if (!ResourceLoader.Exists(path) || ResourceLoader.Load(path) is not Texture2D picture)
        {
            return null;
        }

        float height = Width * picture.GetHeight() / picture.GetWidth();
        return new LabStagePicture(picture, new Vector4(Left, -art.GroundLine * height, Width, height));
    }
}
