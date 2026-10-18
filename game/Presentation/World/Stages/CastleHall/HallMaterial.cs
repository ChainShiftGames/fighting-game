using Godot;

namespace FightingGame.Presentation.World.Stages.CastleHall;

/// <summary>Makes the castle hall's materials from its shaders, with the values that differ from each shader's own.</summary>
public static class HallMaterial
{
    public const string ShaderFolder = "res://Assets/Stages/castle_hall";

    public static ShaderMaterial Of(string shader, params (string Name, Variant Value)[] values)
    {
        var material = new ShaderMaterial { Shader = GD.Load<Shader>($"{ShaderFolder}/{shader}.gdshader") };
        foreach ((string name, Variant value) in values)
        {
            material.SetShaderParameter(name, value);
        }

        return material;
    }

    /// <summary>Dark iron for brackets, chains and chandelier rings.</summary>
    public static StandardMaterial3D Iron() => new() { AlbedoColor = new Color(0.12f, 0.1f, 0.09f), Roughness = 0.7f };
}
