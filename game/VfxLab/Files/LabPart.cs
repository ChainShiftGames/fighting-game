namespace FightingGame.VfxLab.Files;

/// <summary>One part of a variant: the shader that draws it, its card and its clock.</summary>
/// <param name="Shader">A <c>res://</c> path to a <c>.gdshader</c>.</param>
/// <param name="CardWidth">The camera-facing card's size and centre, in metres; it must hold everything the shader draws.</param>
/// <param name="Spin">How far the shader's <c>spin</c> turns per frame, in radians.</param>
/// <param name="GrowFrames">Flight only: frames to grow from <paramref name="StartScale"/> to full size.</param>
/// <param name="StartScale">Flight only: its size on its first frame; the charge's seed grows to this.</param>
public sealed record LabPart(
    string Shader,
    float CardWidth,
    float CardHeight,
    float CentreX,
    float CentreY,
    float Spin,
    float GrowFrames,
    float StartScale);
