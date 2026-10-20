#if TOOLS
using System.Collections.Generic;
using System.Globalization;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>
/// The Overview's facts in plain words (<see cref="MoveFacts"/>): when it comes out, damage, stun, who moves first
/// afterwards and how many moves it chains into. Basic moves get their length and the states that play them.
/// </summary>
public static class DrawerFacts
{
    public static IReadOnlyList<FactList.Fact> Of(DrawerMove move)
    {
        MoveFacts facts = MoveFacts.Of(move.Root);
        if (move.IsBasic)
        {
            return Basic(move, facts);
        }

        List<FactList.Fact> rows = [];
        if (facts.ComesOutOn is int first)
        {
            rows.Add(FactList.Plain(facts.HitsFromProjectile ? "Projectile appears on" : "Comes out after",
                facts.HitsFromProjectile ? "frame of the move" : "wind-up", Frames(first)));
        }
        else if (facts.Phases is FramePhases phases)
        {
            rows.Add(FactList.Plain("Lasts", "it doesn't hit", Frames(phases.TotalFrames)));
        }

        if (facts.Damage is int damage)
        {
            string share = move.Health is int health and > 0
                ? string.Create(CultureInfo.InvariantCulture, $"{damage * 100.0 / health:0.#}% of {move.CharacterName}'s health")
                : "";
            rows.Add(FactList.Plain("Damage", share, Number(damage)));
        }

        if (facts.Hitstun is int hitstun)
        {
            rows.Add(FactList.Plain("Opponent is stunned for", "on hit / when blocked",
                $"{Number(hitstun)} / {(facts.Blockstun is int blockstun ? Number(blockstun) : "–")} f"));
        }

        if (facts.Damage is not null)
        {
            rows.Add(new FactList.Fact("Who moves first after", Advantage(facts),
                [Signed(facts.OnHit), (" / ", MapPalette.Muted), Signed(facts.OnBlock)]));
        }

        rows.Add(FactList.Plain("Chains into", "if it touched the opponent",
            Number(MoveChains.From(move.Id, move.Root).Count)));
        return rows;
    }

    private static List<FactList.Fact> Basic(DrawerMove move, MoveFacts facts)
    {
        List<string> states = [];
        foreach (KeyValuePair<string, System.Text.Json.Nodes.JsonNode?> state in
            move.Document.Character.Root["states"] as System.Text.Json.Nodes.JsonObject ?? [])
        {
            if (JsonNodeValue.Text(state.Value) == move.Id)
            {
                states.Add(state.Key);
            }
        }

        return
        [
            FactList.Plain("Lasts", "then loops or ends", facts.Phases is FramePhases phases ? Frames(phases.TotalFrames) : "–"),
            FactList.Plain("Plays when the fighter is", "the character's states", states.Count == 0 ? "–" : string.Join(", ", states)),
        ];
    }

    private static string Advantage(MoveFacts facts) => facts.HitsFromProjectile
        ? "depends on the distance"
        : facts.OnHit is null ? "on hit: knockdown or launch / when blocked"
        : "on hit / when blocked";

    private static (string, Color) Signed(int? frames) => frames switch
    {
        null => ("–", MapPalette.Muted),
        >= 0 => ("+" + Number(frames.Value), MapPalette.Good),
        _ => ("−" + Number(-frames.Value), MapPalette.Warn),
    };

    private static string Frames(int frames) => Number(frames) + " f";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
#endif
