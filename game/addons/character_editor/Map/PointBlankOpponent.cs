using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;

namespace FightingGame.Addons.CharacterEditor.Map;

/// <summary>
/// Where an opponent standing as close as possible would be: the character's idle hurt boxes, facing left, placed
/// so its body touches the move's body on the frame. Drawn as an outline behind the move, it shows whether a hit
/// box reaches at point blank.
/// </summary>
public static class PointBlankOpponent
{
    /// <summary>The opponent's hurt boxes in the move's coordinates; empty without a body on either side.</summary>
    /// <param name="idle">The character's idle move, which stands in for the opponent.</param>
    public static IReadOnlyList<BoxArea> Of(JsonObject move, int frame, JsonObject? idle)
    {
        if (idle is null || MoveBoxes.Body(move, frame) is not BoxArea body || MoveBoxes.Body(idle, 1) is not BoxArea opponentBody)
        {
            return [];
        }

        // Facing left, a box at x..x+w sits at origin-(x+w)..origin-x; its body's left edge goes on ours' right.
        int origin = body.Right + opponentBody.Right;
        List<BoxArea> hurt = [];
        foreach (JsonObject box in MoveBoxes.OnFrame(idle, 1))
        {
            if (JsonNodeValue.Text(box[MoveFields.Kind]) == MoveBoxes.HurtKind && BoxArea.Of(box) is BoxArea area)
            {
                hurt.Add(area with { X = origin - area.Right });
            }
        }

        return hurt;
    }
}
