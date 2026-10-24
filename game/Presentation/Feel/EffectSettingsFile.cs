using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace FightingGame.Presentation.Feel;

/// <summary>
/// Reads the effect settings file, <c>game/Assets/Effects/effect_settings.json</c> (developer settings, presentation;
/// ADR-0032): every field of <see cref="EffectSettingFields"/>, each in its range. An invalid file is refused with the
/// reason, never fixed up.
/// </summary>
public static class EffectSettingsFile
{
    public const int CurrentVersion = 1;

    /// <summary>The file's path inside the Godot project.</summary>
    public const string ResPath = "res://" + ProjectPath;

    /// <summary>The file's path relative to <c>game/</c>, with '/' separators.</summary>
    public const string ProjectPath = "Assets/Effects/effect_settings.json";

    private const string VersionKey = "schemaVersion";
    private const string SchemaKey = "$schema";

    /// <param name="error">Why the file was refused; empty when it was read.</param>
    /// <returns>The settings, or null when the file is invalid.</returns>
    public static EffectSettings? Read(string json, out string error)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            Dictionary<string, float> values = ReadValues(document.RootElement, out error);
            return error.Length > 0 ? null : Build(values, out error);
        }
        catch (JsonException exception)
        {
            error = exception.Message;
            return null;
        }
    }

    private static Dictionary<string, float> ReadValues(JsonElement root, out string error)
    {
        var values = new Dictionary<string, float>(StringComparer.Ordinal);
        if (root.ValueKind != JsonValueKind.Object)
        {
            error = "the file must hold one JSON object";
            return values;
        }

        if (!root.TryGetProperty(VersionKey, out JsonElement version) || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out int number) || number != CurrentVersion)
        {
            error = $"'{VersionKey}' must be {CurrentVersion}";
            return values;
        }

        error = UnknownKey(root, EffectSettingFields.Sections, VersionKey, SchemaKey);
        foreach (string section in EffectSettingFields.Sections)
        {
            if (error.Length > 0)
            {
                return values;
            }

            error = ReadSection(root, section, values);
        }

        return values;
    }

    private static string ReadSection(JsonElement root, string section, Dictionary<string, float> values)
    {
        if (!root.TryGetProperty(section, out JsonElement fields) || fields.ValueKind != JsonValueKind.Object)
        {
            return $"'{section}' must be an object";
        }

        var keys = new List<string>();
        foreach (EffectSettingField field in EffectSettingFields.All)
        {
            if (field.Section != section)
            {
                continue;
            }

            keys.Add(field.Key);
            if (!fields.TryGetProperty(field.Key, out JsonElement value) || value.ValueKind != JsonValueKind.Number)
            {
                return $"'{field.Path}' must be a number";
            }

            float number = value.GetSingle();
            if (field.Whole && !value.TryGetInt32(out _))
            {
                return $"'{field.Path}' must be a whole number";
            }

            if (number < field.Min || number > field.Max)
            {
                return $"'{field.Path}' is {Text(number)}; it must be {Text(field.Min)} to {Text(field.Max)}";
            }

            values[field.Path] = number;
        }

        return UnknownKey(fields, keys);
    }

    private static string UnknownKey(JsonElement element, IReadOnlyList<string> known, params string[] alsoKnown)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!Contains(known, property.Name) && !Contains(alsoKnown, property.Name))
            {
                return $"unknown field '{property.Name}'";
            }
        }

        return string.Empty;
    }

    private static bool Contains(IReadOnlyList<string> names, string name)
    {
        foreach (string known in names)
        {
            if (string.Equals(known, name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static EffectSettings? Build(Dictionary<string, float> values, out string error)
    {
        int Whole(EffectSettingField field) => (int)values[field.Path];
        float Number(EffectSettingField field) => values[field.Path];

        var sparks = new SparkFeelSettings(
            Whole(EffectSettingFields.SparkLightHitstop),
            Whole(EffectSettingFields.SparkHeavyHitstop),
            Number(EffectSettingFields.SparkCounterWeight),
            Number(EffectSettingFields.SparkBlockShare),
            Number(EffectSettingFields.SparkTearFrom),
            Number(EffectSettingFields.SparkTechWeight),
            Number(EffectSettingFields.SparkScale));
        if (sparks.HeavyHitstop <= sparks.LightHitstop)
        {
            error = $"'{EffectSettingFields.SparkHeavyHitstop.Path}' must be above "
                + $"'{EffectSettingFields.SparkLightHitstop.Path}'";
            return null;
        }

        error = string.Empty;
        return new EffectSettings(
            new ShakeSettings(
                Whole(EffectSettingFields.ShakeLifetime),
                Number(EffectSettingFields.ShakeHit),
                Number(EffectSettingFields.ShakeCounter),
                Number(EffectSettingFields.ShakeThrow),
                Number(EffectSettingFields.ShakeKnockOut),
                Number(EffectSettingFields.ShakeVertical),
                Number(EffectSettingFields.ShakeDefender)),
            sparks,
            new RumbleSettings(
                Number(EffectSettingFields.RumbleHeavy),
                Number(EffectSettingFields.RumbleLight),
                Number(EffectSettingFields.RumbleHitSeconds),
                Number(EffectSettingFields.RumbleKnockOutSeconds)));
    }

    private static string Text(float value) => value.ToString(CultureInfo.InvariantCulture);
}
