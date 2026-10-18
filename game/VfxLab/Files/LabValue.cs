namespace FightingGame.VfxLab.Files;

/// <summary>One setting's value in a sheet or feedback file (a number, toggle, colour or vector), held as floats.</summary>
/// <param name="Numbers">One number for a number or toggle (0 or 1), r g b a for a colour, the components of a vector.</param>
public readonly record struct LabValue(LabValueFormat Format, float[] Numbers)
{
    public float First => Numbers.Length > 0 ? Numbers[0] : 0f;

    public static LabValue Number(float value) => new(LabValueFormat.Number, [value]);

    public static LabValue Toggle(bool on) => new(LabValueFormat.Toggle, [on ? 1f : 0f]);

    public static LabValue Colour(float red, float green, float blue, float alpha) =>
        new(LabValueFormat.Colour, [red, green, blue, alpha]);

    public static LabValue Vector(params float[] components) => new(LabValueFormat.Vector, components);

    /// <summary>The same value with component <paramref name="index"/> set to <paramref name="number"/>.</summary>
    public LabValue WithComponent(int index, float number)
    {
        float[] numbers = (float[])Numbers.Clone();
        numbers[index] = number;
        return this with { Numbers = numbers };
    }
}
