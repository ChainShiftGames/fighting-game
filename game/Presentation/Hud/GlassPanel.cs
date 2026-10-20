using Godot;

namespace FightingGame.Presentation.Hud;

/// <summary>One frosted-glass HUD panel (a bar track, the timer disc, a meter segment): blurs and tints the stage behind it.</summary>
public partial class GlassPanel : ColorRect
{
    private const string ShaderPath = "res://Assets/Shaders/hud_glass.gdshader";

    private readonly ShaderMaterial _material = new() { Shader = GD.Load<Shader>(ShaderPath) };

    public GlassPanel()
    {
        Material = _material;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <param name="cornerShare">Corner radius as a share of the shorter side: 0.5 makes a pill or a disc.</param>
    public void Place(Rect2 rect, float cornerShare)
    {
        Position = rect.Position;
        Size = rect.Size;
        _material.SetShaderParameter("panel_size", rect.Size);
        _material.SetShaderParameter("corner_radius", Mathf.Min(rect.Size.X, rect.Size.Y) * cornerShare);
    }
}
