namespace Sim.Data.Rules;

/// <summary>The round values of <c>rules/match.json</c> (developer settings); the rules are in <c>Sim.Match</c>.</summary>
/// <param name="TimerFrames">Round timer length in frames (99 s = 5940).</param>
/// <param name="RoundsToWin">Round wins that win the match (2 = best of 3).</param>
/// <param name="MaxRounds">The last round a match can reach before it is a draw.</param>
/// <param name="RoundOverFrames">Frames between a KO or time-out and the next round.</param>
public sealed record RoundFormat(int TimerFrames, int RoundsToWin, int MaxRounds, int RoundOverFrames);
