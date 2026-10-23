using FightingGame.Debug;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Presentation;
using FightingGame.Presentation.Events;
using FightingGame.Presentation.Feel;
using Netcode.Drivers;
using Netcode.Replays;
using Sim.Data;
using Sim.State;

namespace FightingGame.Online;

/// <summary>Builds a local two-player match of the real game logic, composed from the loaded game data, and records it.</summary>
public sealed class LocalMatchStartup
{
    private readonly PlayerDevicesChoice _devices;
    private readonly InputDeviceReader _reader;
    private readonly MatchLoopFactory _loops;
    private readonly FrameStepping _stepping;
    private readonly DeveloperToggle _hitboxOverlay;
    private readonly EffectToggles _effectToggles;

    /// <param name="devices">Which devices each player reads (ADR-0038).</param>
    /// <param name="stepping">Pause and frame step (developer setting).</param>
    /// <param name="hitboxOverlay">Draws every box on top of the fighters (developer setting).</param>
    /// <param name="effectToggles">Switches each kind of event effect (developer settings).</param>
    public LocalMatchStartup(
        PlayerDevicesChoice devices,
        InputDeviceReader reader,
        MatchLoopFactory loops,
        FrameStepping stepping,
        DeveloperToggle hitboxOverlay,
        EffectToggles effectToggles)
    {
        _devices = devices;
        _reader = reader;
        _loops = loops;
        _stepping = stepping;
        _hitboxOverlay = hitboxOverlay;
        _effectToggles = effectToggles;
    }

    public StartedMatch Create(GameData data)
    {
        MatchLineup lineup = MatchLineup.From(data);
        var recorder = new ReplayRecorder(lineup.CreateReplayHeader(data));

        PlayerDevices assignment = _devices.Versus();
        var effects = new MatchEffects(
            _effectToggles, new JoypadRumbleMotor(_devices.Roster, assignment, [0, 1]), EffectSettingsChoice.Load());

        // The effects observe the recorded logic: both wrappers see every frame (ADR-0018 §4).
        var driver = new LocalMatchDriver<MatchState>(effects.Observe(lineup.RecordedSimulation(recorder)));
        PlayerInputReader[] players = PlayerInputReader.All(assignment, _reader);
        var view = new MatchView();
        view.Show(driver, lineup.Characters, lineup.Simulation.Rules, lineup.Screen, _hitboxOverlay, effects);
        var disconnect = new ControllerDisconnectOverlay();
        disconnect.Watch(assignment, _devices.Roster, hold => _stepping.IsWaitingForController = hold);
        view.AddChild(disconnect);

        MatchLoop<MatchState> loop = _loops.Create(driver, players, _stepping);
        var recording = new MatchRecording(recorder, driver, confirmed: null, kind: "local");
        return new StartedMatch(
            loop, driver, view, () => $"local {lineup.Description}", connection: null, recording,
            isOver: () => driver.State.Round.Phase == RoundPhase.MatchOver);
    }
}
