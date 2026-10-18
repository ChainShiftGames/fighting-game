using FightingGame.VfxLab.Files;
using Godot;

namespace FightingGame.VfxLab.View;

/// <summary>Turns Lab values into the Godot values a shader uniform of each type takes, and back.</summary>
public static class LabValueVariants
{
    public static Variant ToVariant(LabValue value, UniformSpec spec)
    {
        float[] n = value.Numbers;
        float At(int i) => i < n.Length ? n[i] : 0f;
        return spec.Kind switch
        {
            UniformControlKind.WholeSlider => Variant.From((int)Mathf.Round(At(0))),
            UniformControlKind.Toggle => Variant.From(At(0) > 0.5f),
            UniformControlKind.Colour => Variant.From(new Color(At(0), At(1), At(2), n.Length > 3 ? n[3] : 1f)),
            UniformControlKind.Vector => spec.Default.Numbers.Length switch
            {
                2 => Variant.From(new Vector2(At(0), At(1))),
                3 => Variant.From(new Vector3(At(0), At(1), At(2))),
                _ => Variant.From(new Vector4(At(0), At(1), At(2), At(3))),
            },
            _ => Variant.From(At(0)),
        };
    }

    /// <summary>The Lab value of a uniform's default; zeros of the right size when the shader gives none.</summary>
    public static LabValue FromVariant(Variant value, Variant.Type type) => type switch
    {
        Variant.Type.Bool => LabValue.Toggle(value.VariantType == Variant.Type.Bool && value.AsBool()),
        Variant.Type.Int => LabValue.Number(value.VariantType == Variant.Type.Int ? value.AsInt32() : 0),
        Variant.Type.Color when value.VariantType == Variant.Type.Color => FromColor(value.AsColor()),
        Variant.Type.Color => LabValue.Colour(0f, 0f, 0f, 1f),
        Variant.Type.Vector2 => FromVector2(value.VariantType == Variant.Type.Vector2 ? value.AsVector2() : Vector2.Zero),
        Variant.Type.Vector3 => FromVector3(value.VariantType == Variant.Type.Vector3 ? value.AsVector3() : Vector3.Zero),
        Variant.Type.Vector4 => FromVector4(value.VariantType == Variant.Type.Vector4 ? value.AsVector4() : Vector4.Zero),
        _ => LabValue.Number(value.VariantType == Variant.Type.Float ? value.AsSingle() : 0f),
    };

    private static LabValue FromColor(Color c) => LabValue.Colour(c.R, c.G, c.B, c.A);

    private static LabValue FromVector2(Vector2 v) => LabValue.Vector(v.X, v.Y);

    private static LabValue FromVector3(Vector3 v) => LabValue.Vector(v.X, v.Y, v.Z);

    private static LabValue FromVector4(Vector4 v) => LabValue.Vector(v.X, v.Y, v.Z, v.W);
}
