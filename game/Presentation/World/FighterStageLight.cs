using Godot;

namespace FightingGame.Presentation.World;

/// <summary>Gives the fighters' toon shader the stage's light: tints on its lit and shade tones and a sky-coloured rim.</summary>
/// <remarks>
/// The fighters keep their own fixed light (the Xrd look); these only colour it, so they sit in the stage instead of
/// on top of it. Shader globals, because the fighters' materials are shared files the stage must not edit. Developer
/// settings (presentation), set per stage in its scene; leaving the stage puts the neutral values back.
/// </remarks>
public partial class FighterStageLight : Node
{
    private const string LightTint = "stage_light_tint";
    private const string ShadeTint = "stage_shade_tint";
    private const string RimColor = "stage_rim_color";

    [Export] public Color LitTint { get; set; } = Colors.White;
    [Export] public Color ShadowTint { get; set; } = Colors.White;
    [Export] public Color Rim { get; set; } = Colors.Black;

    public override void _EnterTree() => Apply(LitTint, ShadowTint, Rim);

    public override void _ExitTree() => Apply(Colors.White, Colors.White, Colors.Black);

    private static void Apply(Color lit, Color shade, Color rim)
    {
        RenderingServer.GlobalShaderParameterSet(LightTint, lit);
        RenderingServer.GlobalShaderParameterSet(ShadeTint, shade);
        RenderingServer.GlobalShaderParameterSet(RimColor, rim);
    }
}
