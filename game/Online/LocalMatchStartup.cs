using FightingGame.Debug;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Presentation;
using Netcode.Drivers;
using Sim.Data;
using Sim.State;

namespace FightingGame.Online;

/// <summary>Builds a local two-player match of the real game logic, composed from the loaded game data.</summary>
public sealed class LocalMatchStartup
{
    private readonly PlayerBindings[] _bindings;
    private readonly IDeviceState _devices;
    private readonly JoypadRoster _joypads;
    private readonly MatchLoopFactory _loops;
    private readonly FrameStepping _stepping;
    private readonly DeveloperToggle _hitboxOverlay;

    /// <param name="bindings">One per player on this PC (player settings).</param>
    /// <param name="stepping">Pause and frame step (developer setting).</param>
    /// <param name="hitboxOverlay">Draws every box on top of the fighters (developer setting).</param>
    public LocalMatchStartup(
        PlayerBindings[] bindings,
        IDeviceState devices,
        JoypadRoster joypads,
        MatchLoopFactory loops,
        FrameStepping stepping,
        DeveloperToggle hitboxOverlay)
    {
        _bindings = bindings;
        _devices = devices;
        _joypads = joypads;
        _loops = loops;
        _stepping = stepping;
        _hitboxOverlay = hitboxOverlay;
    }

    public StartedMatch Create(GameData data)
    {
        MatchLineup lineup = MatchLineup.From(data);
        var driver = new LocalMatchDriver<MatchState>(new ComposedMatchSimulation(lineup.Simulation));
        PlayerInputReader[] players = [LocalPlayer(0), LocalPlayer(1)];
        var view = new MatchView();
        view.Show(driver, lineup.Characters, lineup.Walls, _hitboxOverlay);

        MatchLoop<MatchState> loop = _loops.Create(driver, players, _stepping);
        return new StartedMatch(loop, driver, view, $"local {lineup.Description}", connection: null);
    }

    // Player n uses their own keyboard bindings and the joypad in slot n.
    private PlayerInputReader LocalPlayer(int player) => new(
        new KeyboardDevice(_bindings[player].Keyboard, _devices),
        new GamepadDevice(_bindings[player].Gamepad, _devices, _joypads, slot: player));
}
