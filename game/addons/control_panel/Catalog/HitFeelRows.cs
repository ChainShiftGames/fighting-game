using System.Collections.Generic;
using System.Text.Json;
using FightingGame.Addons.ControlPanel.Documents;
using Sim.Data.Characters.Reading;

namespace FightingGame.Addons.ControlPanel.Catalog;

/// <summary>Finds every hit of a character's moves, in the order its <c>character.json</c> lists the moves.</summary>
/// <remarks>Projectile hits aren't listed; they are edited in the move file (Character tab).</remarks>
public static class HitFeelRows
{
    /// <returns>One row per hit; moves without hits (walks, hitstun…) have none.</returns>
    /// <exception cref="JsonException">A file isn't valid JSON.</exception>
    /// <exception cref="System.IO.IOException">A file can't be read.</exception>
    public static IReadOnlyList<HitRow> For(PanelDocument document, string characterId)
    {
        var rows = new List<HitRow>();
        PanelFile character = PanelFile.InData(CharacterSchema.CharacterFile(characterId));
        using JsonDocument list = JsonDocument.Parse(document.Text(character));
        if (!list.RootElement.TryGetProperty("moves", out JsonElement moves) || moves.ValueKind != JsonValueKind.Array)
        {
            return rows;
        }

        foreach (JsonElement id in moves.EnumerateArray())
        {
            if (id.GetString() is not string moveId)
            {
                continue;
            }

            PanelFile file = PanelFile.InData(CharacterSchema.MoveFile(characterId, moveId));
            if (document.Exists(file))
            {
                AddHits(rows, document, moveId, file);
            }
        }

        return rows;
    }

    private static void AddHits(List<HitRow> rows, PanelDocument document, string moveId, PanelFile file)
    {
        using JsonDocument move = JsonDocument.Parse(document.Text(file));
        JsonElement root = move.RootElement;
        if (!root.TryGetProperty("hits", out JsonElement hits) || hits.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        string category = root.TryGetProperty("category", out JsonElement value) ? value.GetString() ?? "" : "";
        int count = hits.GetArrayLength();
        for (int hit = 0; hit < count; hit++)
        {
            rows.Add(new HitRow(moveId, category, hit, count, file));
        }
    }
}
