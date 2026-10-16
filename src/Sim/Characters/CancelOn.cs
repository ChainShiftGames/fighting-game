using System;

namespace Sim.Characters;

/// <summary>Which outcomes of the move open a cancel window; also what the playing move has done so far.</summary>
[Flags]
public enum CancelOn
{
    None = 0,
    Hit = 1 << 0,
    Block = 1 << 1,
    Whiff = 1 << 2,

    /// <summary>
    /// The move met the opponent's attack on the same frame under <c>TradeRule.Clash</c> and is spent. Only an
    /// outcome for now: character data can't open a cancel window on it.
    /// </summary>
    Clash = 1 << 3,
}
