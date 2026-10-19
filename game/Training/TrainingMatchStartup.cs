using FightingGame.Debug;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Online;
using FightingGame.Presentation;
using FightingGame.Presentation.Events;
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

    private readonly PlayerBindings _bindings;
    private readonly IDeviceState _devices;
    private readonly JoypadRoster _joypads;
    private readonly MatchLoopFactory _loops;
    private readonly FrameStepping _stepping;
    private readonly DeveloperToggle _hitboxOverlay;
    private readonly EffectToggles _effectToggles;
    private readonly CurrentTrainingSettings _settings;
    private readonly TrainingMenu _menu;

    // Kept across training matches (restarts, reloaded data) while the game runs; never saved (P3-32).
    private readonly RecordingSlots _recordings = new();

    /// <param name="bindings">Player 1's bindings (player settings): the training player.</param>
    public TrainingMatchStartup(
        PlayerBindings bindings,
        IDeviceState devices,
        JoypadRoster joypads,
        MatchLoopFactory loops,
        FrameStepping stepping,
        DeveloperToggle hitboxOverlay,
        EffectToggles effectToggles,
        CurrentTrainingSettings settings,
        TrainingMenu menu)
    {
        _bindings = bindings;
        _devices = devices;
        _joypads = joypads;
        _loops = loops;
        _stepping = stepping;
        _hitboxOverlay = hitboxOverlay;
        _effectToggles = effectToggles;
        _settings = settings;
        _menu = menu;
    }

    public StartedMatch Create(GameData data)
    {
        MatchLineup lineup = MatchLineup.From(data);
        var timings = new TrainingTimings(GuardResetFrames, HealthRefillDelayFrames);
        var effects = new MatchEffects(_effectToggles, new JoypadRumbleMotor(_joypads, [0, JoypadRumbleMotor.NoSlot]));

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

        PlayerInputReader player = new(
            new KeyboardDevice(_bindings.Keyboard, _devices),
            new GamepadDevice(_bindings.Gamepad, _devices, _joypads, slot: 0));
        MatchLoop<MatchState> loop = _loops.Create(driver, [player], _stepping);
        return new StartedMatch(loop, driver, view, () => $"training {lineup.Description}", connection: null);
    }
}
