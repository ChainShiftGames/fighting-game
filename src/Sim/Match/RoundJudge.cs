using Sim.Characters;
using Sim.Fighters.States;
using Sim.State;

namespace Sim.Match;

/// <summary>Decides who won a round, by KO or at time-out.</summary>
/// <remarks>
/// Both fighters are judged from the same state by the same rule, so neither port wins a tie:
/// - KO: a fighter at 0 health is KO'd and the other wins. Both at 0 on the same frame is a double KO: a draw.
/// - Time-out: the larger share of max health left wins, so characters with different max health are judged
///   fairly (compared by cross-multiplying, in 64 bits so nothing overflows). Equal shares: a draw.
/// </remarks>
public sealed class RoundJudge
{
    private readonly int _player1MaxHealth;
    private readonly int _player2MaxHealth;

    public RoundJudge(CharacterData player1, CharacterData player2)
    {
        _player1MaxHealth = player1.Stats.Health;
        _player2MaxHealth = player2.Stats.Health;
    }

    /// <summary>The fighters still standing if anyone is KO'd (both after a double KO); none if nobody is.</summary>
    public static Winners KnockOutWinners(in FighterStates fighters)
    {
        bool isPlayer1Out = KnockOut.IsKnockedOut(in fighters[0]);
        bool isPlayer2Out = KnockOut.IsKnockedOut(in fighters[1]);
        if (isPlayer1Out == isPlayer2Out)
        {
            return isPlayer1Out ? Winners.Both : Winners.None;
        }

        return Winners.Only(isPlayer1Out ? 1 : 0);
    }

    /// <summary>Who has the larger share of max health left; both if the shares are equal.</summary>
    public Winners TimeOutWinners(in FighterStates fighters)
    {
        long player1Share = (long)fighters[0].Health * _player2MaxHealth;
        long player2Share = (long)fighters[1].Health * _player1MaxHealth;
        if (player1Share == player2Share)
        {
            return Winners.Both;
        }

        return Winners.Only(player1Share > player2Share ? 0 : 1);
    }
}
