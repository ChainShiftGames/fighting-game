using FightingGame.Online;

namespace FightingGame.UI.CharacterSelect;

/// <summary>Each player's last character and colour this session, so character select starts there again.</summary>
public sealed class PickMemory
{
    private readonly FighterPick[] _picks = [FighterPick.Default, FighterPick.Default];

    /// <summary>Player <paramref name="side"/>'s last pick; <see cref="FighterPick.Default"/> before their first.</summary>
    public FighterPick Of(int side) => _picks[side];

    public void Remember(int side, FighterPick pick) => _picks[side] = pick;
}
