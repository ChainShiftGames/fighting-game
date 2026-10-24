using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FightingGame.Input.Profiles;

/// <summary>
/// The layouts that ship with the game (<c>default_bindings.jsonc</c>, embedded): the first of each kind is that
/// kind's default. Shipped data, so a problem in it fails loudly; the tests read it against the real button set.
/// </summary>
public sealed class BuiltInPresets
{
    private readonly List<ControlPreset> _presets;

    private BuiltInPresets(List<ControlPreset> presets)
    {
        _presets = presets;
    }

    public IReadOnlyList<ControlPreset> All => _presets;

    /// <exception cref="InvalidDataException">The embedded file has a problem, or lacks a kind's default.</exception>
    public static BuiltInPresets Load(BindingTargets targets) => Parse(DefaultBindings.ReadJson(), targets);

    /// <exception cref="InvalidDataException">The file has a problem, or lacks a kind's default.</exception>
    public static BuiltInPresets Parse(string json, BindingTargets targets)
    {
        var warnings = new List<string>();
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Built-in controller layouts: not valid JSON: {exception.Message}", exception);
        }

        var presets = new BuiltInPresets(ControlPresetJson.Read(root?["presets"], targets, null, "presets", warnings));
        foreach (InputDeviceKind kind in Enum.GetValues<InputDeviceKind>())
        {
            if (presets.FirstOf(kind) is null)
            {
                warnings.Add($"presets: no {kind} layout");
            }
        }

        return warnings.Count == 0
            ? presets
            : throw new InvalidDataException($"Built-in controller layouts: {string.Join("; ", warnings)}");
    }

    /// <summary>The kind's default layout.</summary>
    public BindingProfile DefaultOf(InputDeviceKind kind) => FirstOf(kind)!.Profile;

    private ControlPreset? FirstOf(InputDeviceKind kind) => _presets.Find(preset => preset.Profile.Kind == kind);
}
