using FightingGame.Online;

namespace FightingGame.UI.CharacterSelect;

/// <summary>
/// Character select against another PC (ADR-0042): which side this PC picks for, the other side's pick once it
/// arrives, and where this PC's pick goes.
/// </summary>
public interface IOnlinePicking
{
    /// <summary>The side (0 or 1) this PC's player picks for.</summary>
    int LocalSide { get; }

    /// <summary>The other PC's pick once it arrived; null until then.</summary>
    FighterPick? OpponentPick { get; }

    /// <summary>Whether this PC's pick went out; it can't be taken back.</summary>
    bool HasSent { get; }

    void Send(FighterPick pick);

    /// <summary>Gives up on this match: leaves the lobby.</summary>
    void Leave();
}
