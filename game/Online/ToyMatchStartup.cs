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
    private readonly float _stickDeadzone;

    /// <param name="stickDeadzone">Stick travel before it counts as a direction (player setting).</param>
    public ToyMatchStartup(ToyRules rules, float stickDeadzone)
    {
        _rules = rules;
        _stickDeadzone = stickDeadzone;
    }

    public ToyMatch CreateLocal()
    {
        PlayerInputReader[] players =
        [
            new(new KeyboardDevice(KeyboardLayout.Player1Default), new GamepadDevice(0, _stickDeadzone)),
            new(new KeyboardDevice(KeyboardLayout.Player2Default), new GamepadDevice(1, _stickDeadzone)),
        ];
        return new ToyMatch(new LocalMatchDriver<ToyState>(Simulation()), players, "local", Connection: null);
    }

    /// <param name="opponentName">Shown in the overlay; the address when there is no name.</param>
    public ToyMatch CreateOnline(PeerMatchSettings settings, PeerTransport transport, string opponentName)
    {
        // One player on this PC, on either keyboard layout or the first gamepad.
        PlayerInputReader[] player =
        [
            new(
                new KeyboardDevice(KeyboardLayout.Player1Default),
                new KeyboardDevice(KeyboardLayout.Player2Default),
                new GamepadDevice(0, _stickDeadzone)),
        ];
        RollbackMatchDriver<ToyState> driver = PeerSession.CreateDriver(Simulation(), settings, transport);
        var connection = new ConnectionMeter(driver, transport.Link, Stopwatch.Frequency);
        return new ToyMatch(driver, player, Describe(settings, opponentName), connection);
    }

    private ToyMatchSimulation Simulation() => new(new ToySimulation(_rules));

    private static string Describe(PeerMatchSettings settings, string opponentName) => string.Create(
        CultureInfo.InvariantCulture,
        $"online P{(settings.LocalIsPlayer1 ? 1 : 2)} vs {opponentName}, " +
        $"rollback {settings.RollbackFrames} f, delay {settings.InputDelayFrames} f, " +
        $"sim {settings.NetworkSimulation.PingMilliseconds} ms ±{settings.NetworkSimulation.JitterMilliseconds} ms " +
        $"{settings.NetworkSimulation.PacketLossPercent} % loss");
}
