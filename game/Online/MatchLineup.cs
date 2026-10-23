using Netcode.Drivers;
using Netcode.Replays;
using Sim.Characters;
using Sim.Composition;
using Sim.Data;
using Sim.Rules;
using Sim.State;
using LogicScreen = Sim.Camera.CameraFraming;

namespace FightingGame.Online;

/// <summary>
/// Who plays whom under which rules, composed from the loaded game data. Both PCs of an online match build the same
/// lineup from the same data, so their matches start identical; a replay rebuilds the lineup it recorded.
/// </summary>
public sealed class MatchLineup
{
    private readonly byte[] _colours;

    private MatchLineup(CharacterData[] characters, byte[] colours, MatchSimulation simulation, LogicScreen screen)
    {
        Characters = characters;
        _colours = colours;
        Simulation = simulation;
        Screen = screen;
    }

    /// <summary>P1's character, then P2's.</summary>
    public CharacterData[] Characters { get; }

    public MatchSimulation Simulation { get; }

    /// <summary>The logic camera's width and the stage walls, from the match rules.</summary>
    public LogicScreen Screen { get; }

    /// <summary>One line naming both characters, e.g. "testman vs testman".</summary>
    public string Description => $"{Characters[0].Id} vs {Characters[1].Id}";

    /// <summary>The default lineup: P1 plays the first character (by id) and P2 the second, or a mirror with only one.</summary>
    public static MatchLineup From(GameData data) => From(data, FighterPick.Default, FighterPick.Default);

    /// <summary>The characters the menus picked; a player without a pick plays the default lineup's character.</summary>
    public static MatchLineup From(GameData data, FighterPick player1, FighterPick player2)
    {
        CharacterData default1 = data.Characters[0];
        CharacterData default2 = data.Characters[data.Characters.Count > 1 ? 1 : 0];

        // Includes the module lineup from data/rules/modules.json (P3-01).
        MatchRules rules = data.CreateMatchRules();
        return Compose(
            data,
            player1.CharacterId is { } id1 ? data.Character(id1) : default1,
            player2.CharacterId is { } id2 ? data.Character(id2) : default2,
            [player1.Colour, player2.Colour],
            in rules);
    }

    /// <summary>The match a replay recorded: its characters, under the match rules it was played by (ADR-0018 §3).</summary>
    /// <remarks>Only for a replay <see cref="ReplayFolder.Load"/> accepted, so this build's data is the recording's.</remarks>
    public static MatchLineup FromReplay(GameData data, ReplayHeader header) => Compose(
        data,
        data.Character(header.Player1.CharacterId),
        data.Character(header.Player2.CharacterId),
        [header.Player1.Colour, header.Player2.Colour],
        in header.Rules);

    /// <summary>The replay header for this match, recorded by this build with <paramref name="data"/>.</summary>
    /// <remarks>It holds the exact rules <see cref="Simulation"/> was built with, so playback runs the same match.</remarks>
    public ReplayHeader CreateReplayHeader(GameData data)
    {
        MatchRules rules = Simulation.Rules;
        return new ReplayHeader(
            GameVersion.Current,
            data.Hash.ToBytes(),
            in rules,
            new ReplayPlayer(Characters[0].Id, _colours[0]),
            new ReplayPlayer(Characters[1].Id, _colours[1]));
    }

    /// <summary>The game logic for a match driver, recording both players' inputs of every frame into <paramref name="recorder"/>.</summary>
    /// <remarks>
    /// The recorder sits right around the game logic (ADR-0018 §4); anything presentation adds goes around this, so a
    /// recorded frame is always one the logic ran.
    /// </remarks>
    public IMatchSimulation<MatchState> RecordedSimulation(ReplayRecorder recorder) =>
        new RecordingMatchSimulation(new ComposedMatchSimulation(Simulation), recorder);

    private static MatchLineup Compose(
        GameData data, CharacterData player1, CharacterData player2, byte[] colours, in MatchRules rules)
    {
        var simulation = new MatchSimulation(
            player1, player2, data.Buttons, data.Motions.Motions, data.Motions.Movement, in rules, data.Flags.Names);
        return new MatchLineup([player1, player2], colours, simulation, LogicScreen.From(in rules));
    }
}
