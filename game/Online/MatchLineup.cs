using Netcode.Drivers;
using Netcode.Replays;
using Sim.Characters;
using Sim.Collision;
using Sim.Composition;
using Sim.Data;
using Sim.Rules;
using Sim.State;

namespace FightingGame.Online;

/// <summary>
/// Who plays whom under which rules, composed from the loaded game data. Both PCs of an online match build the same
/// lineup from the same data, so their matches start identical; a replay rebuilds the lineup it recorded.
/// </summary>
public sealed class MatchLineup
{
    // Until there is a colour select, every character plays in its first colour.
    private const byte DefaultColour = 0;

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

        // Includes the module lineup from data/rules/modules.json (P3-01).
        MatchRules rules = data.CreateMatchRules();
        return Compose(data, player1, player2, in rules);
    }

    /// <summary>The match a replay recorded: its characters, under the match rules it was played by (ADR-0018 §3).</summary>
    /// <remarks>Only for a replay <see cref="ReplayFolder.Load"/> accepted, so this build's data is the recording's.</remarks>
    public static MatchLineup FromReplay(GameData data, ReplayHeader header) => Compose(
        data, data.Character(header.Player1.CharacterId), data.Character(header.Player2.CharacterId), in header.Rules);

    /// <summary>The replay header for this match, recorded by this build with <paramref name="data"/>.</summary>
    /// <remarks>It holds the exact rules <see cref="Simulation"/> was built with, so playback runs the same match.</remarks>
    public ReplayHeader CreateReplayHeader(GameData data)
    {
        MatchRules rules = Simulation.Rules;
        return new ReplayHeader(
            GameVersion.Current,
            data.Hash.ToBytes(),
            in rules,
            new ReplayPlayer(Characters[0].Id, DefaultColour),
            new ReplayPlayer(Characters[1].Id, DefaultColour));
    }

    /// <summary>The game logic for a match driver, recording both players' inputs of every frame into <paramref name="recorder"/>.</summary>
    /// <remarks>
    /// The recorder sits right around the game logic (ADR-0018 §4); anything presentation adds goes around this, so a
    /// recorded frame is always one the logic ran.
    /// </remarks>
    public IMatchSimulation<MatchState> RecordedSimulation(ReplayRecorder recorder) =>
        new RecordingMatchSimulation(new ComposedMatchSimulation(Simulation), recorder);

    private static MatchLineup Compose(GameData data, CharacterData player1, CharacterData player2, in MatchRules rules)
    {
        var simulation = new MatchSimulation(
            player1, player2, data.Buttons, data.Motions.Motions, data.Motions.Movement, in rules);
        return new MatchLineup([player1, player2], simulation, StageWalls.From(in rules));
    }
}
