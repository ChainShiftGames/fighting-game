using FightingGame.Addons.ControlPanel.Documents;

namespace FightingGame.Addons.ControlPanel.Catalog;

/// <summary>One hit of one move: a row of the Hit feel table.</summary>
/// <param name="Category">The move's category in its file (<c>normal</c>, <c>special</c>…).</param>
/// <param name="HitIndex">Which entry of the move's <c>hits</c> list, from 0.</param>
/// <param name="HitCount">How many hits the move has.</param>
public sealed record HitRow(string MoveId, string Category, int HitIndex, int HitCount, PanelFile File)
{
    /// <summary>The move ID, with the hit's number for multi-hit moves (<c>236236d #2</c>).</summary>
    public string Label => HitCount > 1 ? $"{MoveId} #{HitIndex + 1}" : MoveId;

    /// <summary>True for the move's last hit, which its frame advantage is measured from.</summary>
    public bool IsLastHit => HitIndex == HitCount - 1;
}
