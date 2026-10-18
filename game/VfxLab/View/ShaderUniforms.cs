using System.Collections.Generic;
using Godot;

namespace FightingGame.VfxLab.View;

/// <summary>Reads the uniforms a shader declares (its includes' too) as the Lab's control specs, in order, with groups.</summary>
public static class ShaderUniforms
{
    /// <param name="ranges">The sheet's slider ranges by uniform name.</param>
    public static List<UniformSpec> Of(Shader shader, IReadOnlyDictionary<string, float[]> ranges)
    {
        List<UniformSpec> specs = [];
        string group = string.Empty;
        foreach (Godot.Collections.Dictionary entry in shader.GetShaderUniformList(getGroups: true))
        {
            string name = entry["name"].AsString();
            PropertyUsageFlags usage = (PropertyUsageFlags)entry["usage"].AsInt64();
            if ((usage & (PropertyUsageFlags.Group | PropertyUsageFlags.Subgroup)) != 0)
            {
                group = name;
                continue;
            }

            Variant.Type type = (Variant.Type)entry["type"].AsInt64();
            Variant fallback = RenderingServer.ShaderGetParameterDefault(shader.GetRid(), name);
            UniformSpec? spec = UniformSpec.From(
                name,
                group,
                type,
                (PropertyHint)entry["hint"].AsInt64(),
                entry["hint_string"].AsString(),
                LabValueVariants.FromVariant(fallback, type),
                ranges.GetValueOrDefault(name));
            if (spec is not null)
            {
                specs.Add(spec);
            }
        }

        return specs;
    }
}
