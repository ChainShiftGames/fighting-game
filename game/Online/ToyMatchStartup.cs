using System.Globalization;
using FightingGame.Input;
using Godot;
using Netcode.Drivers;
using Netcode.Online;
using Sim.Toy;

namespace FightingGame.Online;

/// <summary>Sets up the Spike A toy match: local two-player by default, online when the command line asks for it.</summary>
public static class ToyMatchStartup
{
    public static ToyMatch Create(ToyRules rules, string[] args, float stickDeadzone)
    {
        var simulation = new ToyMatchSimulation(new ToySimulation(rules));
        PeerMatchSettings? online = OnlineLaunchOptions.Parse(args, out string error);
        if (error.Length > 0)
        {
            GD.PushError($"Ignoring online options, starting a local match: {error}");
        }

        if (online is null)
        {
            PlayerInputReader[] localPlayers =
            [
                new(new KeyboardDevice(KeyboardLayout.Player1Default), new GamepadDevice(0, stickDeadzone)),
                new(new KeyboardDevice(KeyboardLayout.Player2Default), new GamepadDevice(1, stickDeadzone)),
            ];
            return new ToyMatch(new LocalMatchDriver<ToyState>(simulation), localPlayers, "local", IsOnline: false);
        }

        // One player on this PC, on either keyboard layout or the first gamepad.
        PlayerInputReader[] onlinePlayer =
        [
            new(
                new KeyboardDevice(KeyboardLayout.Player1Default),
                new KeyboardDevice(KeyboardLayout.Player2Default),
                new GamepadDevice(0, stickDeadzone)),
        ];
        var driver = new RollbackMatchDriver<ToyState>(simulation, PeerSession.Build<ToyState>(online));
        return new ToyMatch(driver, onlinePlayer, Describe(online), IsOnline: true);
    }

    private static string Describe(PeerMatchSettings online) => string.Create(
        CultureInfo.InvariantCulture,
        $"online P{(online.LocalIsPlayer1 ? 1 : 2)} :{online.LocalPort} -> {online.RemoteEndPoint}, " +
        $"rollback {online.RollbackFrames} f, delay {online.InputDelayFrames} f, " +
        $"sim {online.NetworkSimulation.PingMilliseconds} ms ±{online.NetworkSimulation.JitterMilliseconds} ms " +
        $"{online.NetworkSimulation.PacketLossPercent} % loss");
}
