using System.Text.Json;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Addons.ControlPanel.Documents;

namespace FightingGame.Addons.ControlPanel.Catalog;

/// <summary>
/// A move's frame advantage on hit and on block as its file says now, worked out the way the character editor's map
/// does (<see cref="MoveFacts"/>), so a hitstun or blockstun edit shows its effect at once.
/// </summary>
public static class MoveAdvantage
{
    /// <returns>Null for either when the file can't say (knockdowns, throws, projectiles).</returns>
    public static (int? OnHit, int? OnBlock) Of(PanelDocument document, PanelFile moveFile)
    {
        try
        {
            if (JsonNode.Parse(document.Text(moveFile)) is not JsonObject move)
            {
                return (null, null);
            }

            MoveFacts facts = MoveFacts.Of(move);
            return (facts.OnHit, facts.OnBlock);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    /// <summary>+3, -5, 0; "—" when unknown.</summary>
    public static string Text(int? advantage) =>
        advantage is not int frames ? "—"
            : frames > 0 ? $"+{frames}"
            : frames.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
