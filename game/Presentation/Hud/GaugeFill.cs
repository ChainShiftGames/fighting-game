using Godot;

namespace FightingGame.Presentation.Hud;

/// <summary>The glowing fill of a gauge, or a flat strip such as a damage trail, drawn by the HUD fill shader.</summary>
public partial class GaugeFill : ColorRect
{
    private const string ShaderPath = "res://Assets/Shaders/hud_fill.gdshader";

    private readonly ShaderMaterial _material = new() { Shader = GD.Load<Shader>(ShaderPath) };

    public GaugeFill(Color start, Color middle, Color end, Color glow)
    {
        Material = _material;
        MouseFilter = MouseFilterEnum.Ignore;
        _material.SetShaderParameter("start_color", start);
        _material.SetShaderParameter("middle_color", middle);
        _material.SetShaderParameter("end_color", end);
        _material.SetShaderParameter("glow_color", glow);
    }

    /// <param name="bar">The fill itself; the node grows by <paramref name="glowSize"/> around it.</param>
    /// <param name="isFlipped">Runs the gradient right to left.</param>
    public void Place(Rect2 bar, float glowSize, bool isFlipped)
    {
        Visible = bar.Size.X >= 1f;
        Rect2 node = bar.Grow(glowSize);
        Position = node.Position;
        Size = node.Size;
        _material.SetShaderParameter("node_size", node.Size);
        _material.SetShaderParameter("glow_size", glowSize);
        _material.SetShaderParameter("corner_radius", bar.Size.Y / 2f);
        _material.SetShaderParameter("flip", isFlipped ? 1f : 0f);
    }
}
