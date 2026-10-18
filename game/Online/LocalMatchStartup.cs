using FightingGame.Debug;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Presentation;
using FightingGame.Presentation.Events;
using Netcode.Drivers;
using Netcode.Replays;
using Sim.Data;
using Sim.State;

namespace FightingGame.Online;

/// <summary>Builds a local two-player match of the real game logic, composed from the loaded game data, and records it.</summary>
public sealed class LocalMatchStartup
{
    private readonly PlayerBindings[] _bindings;
    private readonly IDeviceState _devices;
    private readonly JoypadRoster _joypads;
    private readonly MatchLoopFactory _loops;
    private readonly FrameStepping _stepping;
    private readonly DeveloperToggle _hitboxOverlay;
    private readonly EffectToggles _effectToggles;

    /// <param name="bindings">One per player on this PC (player settings).</param>
    /// <param name="stepping">Pause and frame step (developer setting).</param>
    /// <param name="hitboxOverlay">Draws every box on top of the fighters (developer setting).</param>
    /// <param name="effectToggles">Switches each kind of event effect (developer settings).</param>
    public LocalMatchStartup(
        PlayerBindings[] bindings,
        IDeviceState devices,
        JoypadRoster joypads,
        MatchLoopFactory loops,
        FrameStepping stepping,
        DeveloperToggle hitboxOverlay,
        EffectToggles effectToggles)
    {
        _bindings = bindings;
        _devices = devices;
        _joypads = joypads;
        _loops = loops;
        _stepping = stepping;
        _hitboxOverlay = hitboxOverlay;
        _effectToggles = effectToggles;
    }

    public StartedMatch Create(GameData data)
    {
        MatchLineup lineup = MatchLineup.From(data);
        var recorder = new ReplayRecorder(lineup.CreateReplayHeader(data));

        // Player n reads the joypad in slot n, so that is the one that rumbles.
        var effects = new MatchEffects(_effectToggles, new JoypadRumbleMotor(_joypads, [0, 1]));

        // The effects observe the recorded logic: both wrappers see every frame (ADR-0018 §4).
        var driver = new LocalMatchDriver<MatchState>(effects.Observe(lineup.RecordedSimulation(recorder)));
        PlayerInputReader[] players = [LocalPlayer(0), LocalPlayer(1)];
        var view = new MatchView();
        view.Show(driver, lineup.Characters, lineup.Screen, _hitboxOverlay, effects);

        MatchLoop<MatchState> loop = _loops.Create(driver, players, _stepping);
        var recording = new MatchRecording(recorder, driver, confirmed: null, kind: "local");
        return new StartedMatch(loop, driver, view, () => $"local {lineup.Description}", connection: null, recording);
    }

    // Player n uses their own keyboard bindings and the joypad in slot n.
    private PlayerInputReader LocalPlayer(int player) => new(
        new KeyboardDevice(_bindings[player].Keyboard, _devices),
        new GamepadDevice(_bindings[player].Gamepad, _devices, _joypads, slot: player));
}
