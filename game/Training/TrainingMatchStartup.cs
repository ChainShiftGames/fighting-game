using FightingGame.Debug;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Online;
using FightingGame.Presentation;
using FightingGame.Presentation.Events;
using FightingGame.Presentation.Feel;
using Netcode.Drivers;
using Netcode.Drivers.Training;
using Sim.Data;
using Sim.State;

namespace FightingGame.Training;

/// <summary>Builds a training match: player 1's devices against the dummy, with the training menu bound to it. Not recorded.</summary>
public sealed class TrainingMatchStartup
{
    // Developer settings.
    private const int GuardResetFrames = 20;
    private const int HealthRefillDelayFrames = 60;

    private readonly PlayerDevicesChoice _devices;
    private readonly InputDeviceReader _reader;
    private readonly MatchLoopFactory _loops;
    private readonly FrameStepping _stepping;
    private readonly DeveloperToggle _hitboxOverlay;
    private readonly EffectToggles _effectToggles;
    private readonly CurrentTrainingSettings _settings;
    private readonly TrainingMenu _menu;

    // Kept across training matches (restarts, reloaded data) while the game runs; never saved (P3-32).
    private readonly RecordingSlots _recordings = new();

    /// <param name="devices">Which devices the training player reads (ADR-0038).</param>
    public TrainingMatchStartup(
        PlayerDevicesChoice devices,
        InputDeviceReader reader,
        MatchLoopFactory loops,
        FrameStepping stepping,
        DeveloperToggle hitboxOverlay,
        EffectToggles effectToggles,
        CurrentTrainingSettings settings,
        TrainingMenu menu)
    {
        _devices = devices;
        _reader = reader;
        _loops = loops;
        _stepping = stepping;
        _hitboxOverlay = hitboxOverlay;
        _effectToggles = effectToggles;
        _settings = settings;
        _menu = menu;
    }

    public StartedMatch Create(GameData data, MatchSetup setup)
    {
        MatchLineup lineup = MatchLineup.From(data, setup.Player1, setup.Player2);
        var timings = new TrainingTimings(GuardResetFrames, HealthRefillDelayFrames);
        PlayerDevices assignment = _devices.Training();
        var effects = new MatchEffects(
            _effectToggles,
            new JoypadRumbleMotor(_devices.Roster, assignment, [0, JoypadRumbleMotor.NoLocalPlayer]),
            EffectSettingsChoice.Load());

        var meter = new FrameMeter();
        IMatchSimulation<MatchState> simulation = new FrameMeterSimulation(
            effects.Observe(new ComposedMatchSimulation(lineup.Simulation)), lineup.Characters, meter);
        var inputs = DummyModeInputs.Create(
            lineup.Characters[TrainingSides.Player],
            timings,
            () => _settings.Value.Dummy,
            _recordings,
            () => _settings.Value.ToPlaybackChoice());
        var driver = new TrainingMatchDriver<MatchState>(
            simulation, inputs, TrainingAdjustments.Create(lineup.Simulation, timings));

        var view = new MatchView();
        view.Show(driver, lineup.Characters, lineup.Simulation.Rules, lineup.Screen, _hitboxOverlay, effects);
        var meterBar = new FrameMeterBar();
        meterBar.Show(meter, () => _settings.Value.ShowFrameMeter);
        view.AddChild(meterBar);
        _menu.Use(TrainingMenuEntries.Create(_settings, new TrainingSession(driver, meter, effects), _recordings));

        var disconnect = new ControllerDisconnectOverlay();
        disconnect.Watch(assignment, _devices.Roster, hold => _stepping.IsWaitingForController = hold);
        view.AddChild(disconnect);

        MatchLoop<MatchState> loop = _loops.Create(driver, PlayerInputReader.All(assignment, _reader), _stepping);
        return new StartedMatch(loop, driver, view, () => $"training {lineup.Description}", connection: null);
    }
}
