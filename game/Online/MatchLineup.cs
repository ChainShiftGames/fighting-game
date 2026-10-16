using Sim.Characters;
using Sim.Collision;
using Sim.Composition;
using Sim.Data;
using Sim.Rules;

namespace FightingGame.Online;

/// <summary>
/// Who plays whom under which rules, composed from the loaded game data. Both PCs of an online match build the same
/// lineup from the same data, so their matches start identical.
/// </summary>
public sealed class MatchLineup
{
    private MatchLineup(CharacterData[] characters, MatchSimulation simulation, StageWalls walls)
    {
        Characters = characters;
        Simulation = simulation;
        Walls = walls;
    }

    /// <summary>P1's character, then P2's.</summary>
    public CharacterData[] Characters { get; }

    public MatchSimulation Simulation { get; }

    public StageWalls Walls { get; }

    /// <summary>One line naming both characters, e.g. "testman vs testman".</summary>
    public string Description => $"{Characters[0].Id} vs {Characters[1].Id}";

    /// <summary>P1 plays the first character (by id) and P2 the second, or a mirror with only one; until character select.</summary>
    public static MatchLineup From(GameData data)
    {
        CharacterData player1 = data.Characters[0];
        CharacterData player2 = data.Characters[data.Characters.Count > 1 ? 1 : 0];

        // No mechanics modules yet: the module lineup in data is P3-01.
        MatchRules rules = data.CreateMatchRules();
        var simulation = new MatchSimulation(
            player1, player2, data.Buttons, data.Motions.Motions, data.Motions.Movement, in rules);
        return new MatchLineup([player1, player2], simulation, StageWalls.From(in rules));
    }
}
