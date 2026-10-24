namespace FightingGame.UI.Results;

/// <summary>How a match ended, as the results screen tells it.</summary>
public enum MatchOutcome
{
    /// <summary>Stopped without a result: a desync, a lost opponent, refused pre-match checks.</summary>
    Unfinished,

    Player1Wins,

    Player2Wins,

    Draw,
}
