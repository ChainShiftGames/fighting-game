using Godot;

namespace FightingGame.Presentation.World;

/// <summary>
/// Switches the fighters' brush look (dry-brush shade borders, light patches, wobbly ink outlines) through a shader
/// global, because the fighters' materials are shared files the view must not edit.
/// </summary>
public sealed class FighterBrush
{
    private static readonly StringName _global = "fighter_brush";
    private bool? _shown;

    /// <summary>Cheap to call every frame.</summary>
    public void Show(bool brushed)
    {
        if (_shown == brushed)
        {
            return;
        }

        _shown = brushed;
        RenderingServer.GlobalShaderParameterSet(_global, brushed ? 1f : 0f);
    }
}
