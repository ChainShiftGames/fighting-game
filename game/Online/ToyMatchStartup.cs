using System.Diagnostics;
using System.Globalization;
using FightingGame.Diagnostics;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Presentation;
using Netcode.Drivers;
using Netcode.Online;
using Sim.Toy;

namespace FightingGame.Online;

/// <summary>
/// Builds the Spike A toy match online against one other PC. Online play stays on the toy simulation until it
/// moves to the real one; local matches already run the real one (<see cref="LocalMatchStartup"/>).
/// </summary>
public sealed class ToyMatchStartup
{
    private readonly ToyRules _rules;
    private readonly PlayerBindings[] _bindings;
    private readonly IDeviceState _devices;
    private readonly JoypadRoster _joypads;
    private readonly MatchLoopFactory _loops;

    /// <param name="bindings">One per player on this PC (player settings).</param>
    public ToyMatchStartup(
        ToyRules rules, PlayerBindings[] bindings, IDeviceState devices, JoypadRoster joypads, MatchLoopFactory loops)
    {
        _rules = rules;
        _bindings = bindings;
        _devices = devices;
        _joypads = joypads;
        _loops = loops;
    }

    /// <param name="opponentName">Shown in the overlay; the address when there is no name.</param>
    public StartedMatch CreateOnline(PeerMatchSettings settings, PeerTransport transport, string opponentName)
    {
        // One player on this PC, on either player's keyboard bindings or the first joypad.
        PlayerInputReader[] player =
        [
            new(
                new KeyboardDevice(_bindings[0].Keyboard, _devices),
                new KeyboardDevice(_bindings[1].Keyboard, _devices),
                new GamepadDevice(_bindings[0].Gamepad, _devices, _joypads, slot: 0)),
        ];
        RollbackMatchDriver<ToyState> driver =
            PeerSession.CreateDriver(new ToyMatchSimulation(new ToySimulation(_rules)), settings, transport);
        var connection = new ConnectionMeter(driver, transport.Link, Stopwatch.Frequency);
        var view = new ToyMatchView();
        view.Show(driver, _rules);

        // An online match can't pause: the other player's game keeps running.
        MatchLoop<ToyState> loop = _loops.Create(driver, player, stepping: null);
        return new StartedMatch(loop, driver, view, Describe(settings, opponentName), connection);
    }

    private static string Describe(PeerMatchSettings settings, string opponentName) => string.Create(
        CultureInfo.InvariantCulture,
        $"online P{(settings.LocalIsPlayer1 ? 1 : 2)} vs {opponentName}, " +
        $"rollback {settings.RollbackFrames} f, delay {settings.InputDelayFrames} f, " +
        $"sim {settings.NetworkSimulation.PingMilliseconds} ms ±{settings.NetworkSimulation.JitterMilliseconds} ms " +
        $"{settings.NetworkSimulation.PacketLossPercent} % loss");
}
