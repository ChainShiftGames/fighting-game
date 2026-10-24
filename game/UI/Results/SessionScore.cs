using FightingGame.UI.Flow;

namespace FightingGame.UI.Results;

/// <summary>
/// Matches won this session by each side ("P1 3 – 1 P2"). Rematches and character select keep it; the main menu, the
/// title and the online screens (a new lobby, maybe a new opponent) start it again. Not part of any game state.
/// </summary>
public sealed class SessionScore
{
    private readonly int[] _wins = new int[2];

    /// <summary>Finished matches counted, draws included.</summary>
    public int Matches { get; private set; }

    public int WinsOf(int side) => _wins[side];

    /// <summary>Whether reaching <paramref name="step"/> ends the session.</summary>
    public static bool EndsAt(FlowStep step) => step is FlowStep.Title or FlowStep.MainMenu or FlowStep.Online;

    /// <summary>Counts a finished match; one without a result doesn't count.</summary>
    public void Record(MatchResult result)
    {
        if (!result.IsFinished)
        {
            return;
        }

        Matches++;
        if (result.WinnerSide >= 0)
        {
            _wins[result.WinnerSide]++;
        }
    }

    public void Reset()
    {
        Matches = 0;
        _wins[0] = 0;
        _wins[1] = 0;
    }
}
