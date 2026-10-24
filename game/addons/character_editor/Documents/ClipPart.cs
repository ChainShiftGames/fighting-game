namespace FightingGame.Addons.CharacterEditor.Documents;

/// <summary>The part of a move's clip that plays (<c>clipFrom</c>..<c>clipTo</c>), in whole clip frames at 60 fps.</summary>
public readonly record struct ClipPart(int Start, int End)
{
    public int Clamp(int clip) => clip < Start ? Start : clip > End ? End : clip;
}
