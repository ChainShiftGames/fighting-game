using System;
using Sim.Characters;
using Sim.Match;
using Sim.Rules;
using Sim.State;

namespace Sim.Tests.Match;

/// <summary>Match rules, characters and a round stage for round tests (Sim.Tests can't load JSON).</summary>
internal sealed class RoundSetup
{
    public const int StartDistance = 150_000;
    public const int Health = 10_000;

    public RoundSetup(
        int timerFrames = 600,
        int roundsToWin = 2,
        int maxRounds = 5,
        int roundOverFrames = 10,
        int player1Health = Health,
        int player2Health = Health)
    {
        Rules = new MatchRules
        {
            RoundStartDistance = StartDistance,
            RoundTimerFrames = timerFrames,
            RoundsToWin = roundsToWin,
            MaxRounds = maxRounds,
            RoundOverFrames = roundOverFrames,
        };
        Player1 = Character(player1Health);
        Player2 = Character(player2Health);
        Start = new RoundStart(Player1, Player2, Rules);
        Stage = new RoundStage(Start, new RoundJudge(Player1, Player2), new MatchScore(Rules), Rules);
    }

    public MatchRules Rules { get; }

    public CharacterData Player1 { get; }

    public CharacterData Player2 { get; }

    public RoundStart Start { get; }

    public RoundStage Stage { get; }

    /// <summary>Round 1 as the match starts it.</summary>
    public MatchState FirstRound()
    {
        MatchState state = default;
        Start.Begin(ref state, 1);
        return state;
    }

    /// <summary>A character with only an idle move (index 0) and the given max health.</summary>
    public static CharacterData Character(int health) => new(
        "round",
        "Round",
        new CharacterStats { Health = health },
        ["idle"],
        [new MoveData { Category = MoveCategory.Basic, TotalFrames = 60 }],
        [],
        [],
        [],
        [],
        [],
        new int[Enum.GetValues<BasicState>().Length]);
}
