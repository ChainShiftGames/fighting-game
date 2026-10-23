using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FightingGame.UI.Framework;

namespace FightingGame.UI.CharacterSelect;

/// <summary>
/// Reads a character's select file, <c>game/Assets/Characters/&lt;id&gt;/&lt;id&gt;_select.json</c>: its colours and
/// the translation key of its one-line description (ADR-0041). Presentation only: not in <c>data/</c>, so not in the
/// data hash or the match rules. An invalid file is refused with the reason, never fixed up.
/// </summary>
public static partial class CharacterSelectFile
{
    public const int CurrentVersion = 1;

    /// <summary>The colour index travels as a byte (<see cref="Online.FighterPick"/>, the replay header).</summary>
    public const int MaxColours = 256;

    /// <summary>A character without a select file: one colour, its look as it is, and no description.</summary>
    public const string PlaceholderColour = "#ffffff";

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>The file's name in the character's art folder.</summary>
    public static string FileNameFor(string characterId) => $"{characterId}_select.json";

    public static SelectableCharacter Placeholder(string id, string name) =>
        new(id, name, MenuText.Empty, [PlaceholderColour]);

    /// <param name="error">Why the file was refused; empty when it was read.</param>
    /// <returns>The character as character select shows it, or null when the file is invalid.</returns>
    public static SelectableCharacter? Read(string id, string name, string json, out string error)
    {
        SelectFileJson? file;
        try
        {
            file = JsonSerializer.Deserialize<SelectFileJson>(json, _json);
        }
        catch (JsonException exception)
        {
            error = exception.Message;
            return null;
        }

        error = Check(file);
        if (error.Length > 0)
        {
            return null;
        }

        MenuText description = string.IsNullOrEmpty(file!.Description) ? MenuText.Empty : MenuText.Key(file.Description);
        return new SelectableCharacter(id, name, description, file.Colours);
    }

    private static string Check(SelectFileJson? file)
    {
        if (file is null)
        {
            return "the file is empty";
        }

        if (file.SelectVersion != CurrentVersion)
        {
            return $"selectVersion is {file.SelectVersion}, this build reads {CurrentVersion}";
        }

        if (file.Colours.Count is 0 or > MaxColours)
        {
            return $"colours needs 1 to {MaxColours} entries, it has {file.Colours.Count}";
        }

        for (int i = 0; i < file.Colours.Count; i++)
        {
            if (!HtmlColour().IsMatch(file.Colours[i]))
            {
                return $"colours[{i}] '{file.Colours[i]}' isn't a #rrggbb colour";
            }
        }

        return file.Description is { } key && !TranslationKey().IsMatch(key)
            ? $"description '{key}' isn't a translation key (capitals and underscores)"
            : string.Empty;
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HtmlColour();

    [GeneratedRegex("^[A-Z][A-Z_]*$")]
    private static partial Regex TranslationKey();

    private sealed class SelectFileJson
    {
        public int SelectVersion { get; set; }

        /// <summary>A key in <c>game/Assets/UI/menu_text.csv</c>.</summary>
        public string? Description { get; set; }

        public List<string> Colours { get; set; } = [];
    }
}
