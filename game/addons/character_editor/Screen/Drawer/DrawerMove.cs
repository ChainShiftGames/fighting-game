#if TOOLS
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>The move the drawer edits, with what its pages need to show and edit it.</summary>
/// <param name="Art">The character's art file, for the animation timing; null when it has none.</param>
public sealed record DrawerMove(
    string Id, EditedFile File, CharacterDocument Document, EditingContext Context, MoveMapSettings Settings,
    ArtTimingFile? Art = null)
{
    /// <summary>The logic's fixed rate (ARCHITECTURE.md §3), for showing frames as seconds.</summary>
    public const int FramesPerSecond = 60;

    public JsonObject Root => File.Root;

    public string Name => MoveNames.Of(Id, Root, Settings);

    public string Category => JsonNodeValue.Text(Root[MoveFields.Category]) ?? "";

    public bool IsBasic => Category == MoveFields.BasicCategory;

    /// <summary>The character's health, for damage as a share of a health bar; null when unreadable.</summary>
    public int? Health => JsonNodeValue.Int(Document.Character.Root["stats"]?["health"]);

    /// <summary>The character's name as players see it.</summary>
    public string CharacterName =>
        JsonNodeValue.Text(Document.Character.Root["displayName"]) ?? MoveNames.Words(Document.Id);

    /// <summary>Moves a cancel window can name: the character's attacks.</summary>
    public IReadOnlyList<string> CancelTargets => MoveCancels.TargetsOf(Document);

    /// <summary>A move id as its plain name, for lists of moves.</summary>
    public string NameOf(string moveId) =>
        Document.Move(moveId) is EditedFile move ? MoveNames.Of(moveId, move.Root, Settings) : moveId;

    public static string Seconds(int frames) =>
        (frames / (double)FramesPerSecond).ToString("0.00", CultureInfo.InvariantCulture) + " s";
}
#endif
