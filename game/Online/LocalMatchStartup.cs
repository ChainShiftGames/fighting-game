using FightingGame.Debug;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Presentation;
using Netcode.Drivers;
using Sim.Characters;
using Sim.Composition;
using Sim.Data;
using Sim.Rules;
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

    /// <summary>P1 plays the first character (by id) and P2 the second, or a mirror with only one; until character select.</summary>
    public StartedMatch Create(GameData data)
    {
        CharacterData player1 = data.Characters[0];
        CharacterData player2 = data.Characters[data.Characters.Count > 1 ? 1 : 0];

        // No mechanics modules yet: none ships besides the contract test probe.
        MatchRules rules = data.CreateMatchRules();
        var match = new MatchSimulation(
            player1, player2, data.Buttons, data.Motions.Motions, data.Motions.Movement, in rules);
        var driver = new LocalMatchDriver<MatchState>(new ComposedMatchSimulation(match));
        PlayerInputReader[] players = [LocalPlayer(0), LocalPlayer(1)];
        var view = new MatchView();
        view.Show(driver, [player1, player2], _hitboxOverlay);

        MatchLoop<MatchState> loop = _loops.Create(driver, players, _stepping);
        return new StartedMatch(loop, driver, view, $"local {player1.Id} vs {player2.Id}", connection: null);
    }

    // Player n uses their own keyboard bindings and the joypad in slot n.
    private PlayerInputReader LocalPlayer(int player) => new(
        new KeyboardDevice(_bindings[player].Keyboard, _devices),
        new GamepadDevice(_bindings[player].Gamepad, _devices, _joypads, slot: player));
}
