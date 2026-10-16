using System.Diagnostics;
using System.Globalization;
using FightingGame.Diagnostics;
using FightingGame.Input;
using Netcode.Drivers;
using Netcode.Online;
using Sim.Toy;

namespace FightingGame.Online;

/// <summary>Builds the Spike A toy match: local two-player, or online against one other PC.</summary>
public sealed class ToyMatchStartup
{
    private readonly ToyRules _rules;
    private readonly PlayerBindings[] _bindings;
    private readonly IDeviceState _devices;
    private readonly JoypadRoster _joypads;

    /// <param name="bindings">One per player on this PC (player settings).</param>
    public ToyMatchStartup(ToyRules rules, PlayerBindings[] bindings, IDeviceState devices, JoypadRoster joypads)
    {
        _rules = rules;
        _bindings = bindings;
        _devices = devices;
        _joypads = joypads;
    }

    public ToyMatch CreateLocal()
    {
        PlayerInputReader[] players = [LocalPlayer(0), LocalPlayer(1)];
        return new ToyMatch(new LocalMatchDriver<ToyState>(Simulation()), players, "local", Connection: null);
    }

    /// <param name="opponentName">Shown in the overlay; the address when there is no name.</param>
    public ToyMatch CreateOnline(PeerMatchSettings settings, PeerTransport transport, string opponentName)
    {
        // One player on this PC, on either player's keyboard bindings or the first joypad.
        PlayerInputReader[] player =
        [
            new(
                new KeyboardDevice(_bindings[0].Keyboard, _devices),
                new KeyboardDevice(_bindings[1].Keyboard, _devices),
                new GamepadDevice(_bindings[0].Gamepad, _devices, _joypads, slot: 0)),
        ];
        RollbackMatchDriver<ToyState> driver = PeerSession.CreateDriver(Simulation(), settings, transport);
        var connection = new ConnectionMeter(driver, transport.Link, Stopwatch.Frequency);
        return new ToyMatch(driver, player, Describe(settings, opponentName), connection);
    }

    // Player n uses their own keyboard bindings and the joypad in slot n.
    private PlayerInputReader LocalPlayer(int player) => new(
        new KeyboardDevice(_bindings[player].Keyboard, _devices),
        new GamepadDevice(_bindings[player].Gamepad, _devices, _joypads, slot: player));

    private ToyMatchSimulation Simulation() => new(new ToySimulation(_rules));

    private static string Describe(PeerMatchSettings settings, string opponentName) => string.Create(
        CultureInfo.InvariantCulture,
        $"online P{(settings.LocalIsPlayer1 ? 1 : 2)} vs {opponentName}, " +
        $"rollback {settings.RollbackFrames} f, delay {settings.InputDelayFrames} f, " +
        $"sim {settings.NetworkSimulation.PingMilliseconds} ms ±{settings.NetworkSimulation.JitterMilliseconds} ms " +
        $"{settings.NetworkSimulation.PacketLossPercent} % loss");
}
