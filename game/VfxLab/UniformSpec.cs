using System;
using System.Collections.Generic;
using System.Globalization;
using FightingGame.VfxLab.Files;
using Godot;

namespace FightingGame.VfxLab;

/// <summary>
/// A shader uniform the Lab gives a control (ADR-0029 §4): its kind, group and slider range. Built from what the
/// shader declares, so adding a knob to an effect is adding a uniform.
/// </summary>
/// <param name="Group">The <c>group_uniforms</c> it is in; empty when none.</param>
/// <param name="Default">The shader's default value.</param>
public sealed record UniformSpec(
    string Name,
    string Group,
    UniformControlKind Kind,
    float Min,
    float Max,
    float Step,
    LabValue Default,
    bool HasAlpha)
{
    // Developer setting: slider steps across a range when nothing says the step.
    private const float StepsAcrossRange = 200f;

    /// <summary>Uniforms the Lab drives every frame itself (the clock and the effect's state); they get no control.</summary>
    public static IReadOnlySet<string> DriverUniforms { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "t", "spin", "size", "progress", "spawn_size", "age", "kind", "lifetime", "card_size", "card_centre",
    };

    /// <param name="sheetRange">The sheet's [min, max(, step)] for this name, used when the shader has no hint_range.</param>
    /// <returns>Null for a uniform the Lab drives or has no control for (textures, matrices, arrays).</returns>
    public static UniformSpec? From(
        string name, string group, Variant.Type type, PropertyHint hint, string hintString, LabValue defaultValue, float[]? sheetRange)
    {
        if (DriverUniforms.Contains(name))
        {
            return null;
        }

        UniformControlKind? kind = type switch
        {
            Variant.Type.Float => UniformControlKind.Slider,
            Variant.Type.Int => UniformControlKind.WholeSlider,
            Variant.Type.Bool => UniformControlKind.Toggle,
            Variant.Type.Color => UniformControlKind.Colour,
            Variant.Type.Vector2 or Variant.Type.Vector3 or Variant.Type.Vector4 => UniformControlKind.Vector,
            _ => null,
        };
        if (kind is not { } control)
        {
            return null;
        }

        (float min, float max, float step) = RangeOf(control, hint, hintString, defaultValue, sheetRange);
        return new UniformSpec(name, group, control, min, max, step, defaultValue, hint != PropertyHint.ColorNoAlpha);
    }

    private static (float Min, float Max, float Step) RangeOf(
        UniformControlKind kind, PropertyHint hint, string hintString, LabValue defaultValue, float[]? sheetRange)
    {
        bool whole = kind == UniformControlKind.WholeSlider;
        if (hint == PropertyHint.Range && TryParseRange(hintString, out float min, out float max, out float step))
        {
            return (min, max, step > 0f ? step : Fallback(min, max, whole));
        }

        if (sheetRange is [float sheetMin, float sheetMax, ..] && sheetMin < sheetMax)
        {
            float sheetStep = sheetRange.Length > 2 ? sheetRange[2] : Fallback(sheetMin, sheetMax, whole);
            return (sheetMin, sheetMax, sheetStep);
        }

        // Nothing says: from 0 (or the lowest component, when below 0) to twice the largest component, at least 1.
        float low = 0f;
        float high = 0f;
        foreach (float number in defaultValue.Numbers)
        {
            low = MathF.Min(low, number * 2f);
            high = MathF.Max(high, MathF.Abs(number) * 2f);
        }

        high = MathF.Max(high, whole ? 4f : 1f);
        return (low, high, Fallback(low, high, whole));
    }

    private static float Fallback(float min, float max, bool whole) => whole ? 1f : (max - min) / StepsAcrossRange;

    private static bool TryParseRange(string hintString, out float min, out float max, out float step)
    {
        min = max = step = 0f;
        List<float> numbers = [];
        foreach (string part in hintString.Split(','))
        {
            if (float.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
            {
                numbers.Add(number);
            }
        }

        if (numbers.Count < 2 || numbers[0] >= numbers[1])
        {
            return false;
        }

        (min, max, step) = (numbers[0], numbers[1], numbers.Count > 2 ? numbers[2] : 0f);
        return true;
    }
}
