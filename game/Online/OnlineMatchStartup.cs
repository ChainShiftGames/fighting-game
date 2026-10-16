using System;
using System.Diagnostics;
using System.Globalization;
using FightingGame.Debug;
using FightingGame.Diagnostics;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Presentation;
using Godot;
using Netcode.Drivers;
using Netcode.Online;
using Sim.Data;
using Sim.Data.Online;
using Sim.State;

namespace FightingGame.Online;

/// <summary>Builds an online match of the real game logic against one other PC, over a Backdash peer session.</summary>
public sealed class OnlineMatchStartup
{
    private readonly PlayerBindings[] _bindings;
    private readonly IDeviceState _devices;
    private readonly JoypadRoster _joypads;
    private readonly MatchLoopFactory _loops;
    private readonly DeveloperToggle _hitboxOverlay;
    private readonly string[] _args;

    /// <param name="bindings">One per player on this PC (player settings).</param>
    /// <param name="hitboxOverlay">Draws every box on top of the fighters (developer setting).</param>
    /// <param name="args">Command line, for the online tuning options of <see cref="OnlineLaunchOptions"/>.</param>
    public OnlineMatchStartup(
        PlayerBindings[] bindings,
        IDeviceState devices,
        JoypadRoster joypads,
        MatchLoopFactory loops,
        DeveloperToggle hitboxOverlay,
        string[] args)
    {
        _bindings = bindings;
        _devices = devices;
        _joypads = joypads;
        _loops = loops;
        _hitboxOverlay = hitboxOverlay;
        _args = args;
    }

    /// <param name="online">The rollback window and other online developer settings from data.</param>
    public StartedMatch Create(GameData data, OnlineSettings online, OnlineOpponent opponent)
    {
        PeerMatchSettings settings = Settings(online, opponent);
        MatchLineup lineup = MatchLineup.From(data);

        // One player on this PC, on either player's keyboard bindings or the first joypad.
        PlayerInputReader[] player =
        [
            new(
                new KeyboardDevice(_bindings[0].Keyboard, _devices),
                new KeyboardDevice(_bindings[1].Keyboard, _devices),
                new GamepadDevice(_bindings[0].Gamepad, _devices, _joypads, slot: 0)),
        ];
        RollbackMatchDriver<MatchState> driver =
            PeerSession.CreateDriver(new ComposedMatchSimulation(lineup.Simulation), settings, opponent.Transport);
        var connection = new ConnectionMeter(driver, opponent.Transport.Link, Stopwatch.Frequency);
        var view = new MatchView();
        view.Show(driver, lineup.Characters, lineup.Walls, _hitboxOverlay);

        // An online match can't pause: the other player's game keeps running.
        MatchLoop<MatchState> loop = _loops.Create(driver, player, stepping: null);
        return new StartedMatch(loop, driver, view, Describe(settings, opponent, lineup, data), connection);
    }

    // The command line's tuning options override the data's values for this match (exported builds can't edit data).
    private PeerMatchSettings Settings(OnlineSettings online, OnlineOpponent opponent)
    {
        var settings = new PeerMatchSettings
        {
            LocalPort = opponent.LocalPort,
            RemoteEndPoint = opponent.Address,
            LocalIsPlayer1 = opponent.LocalIsPlayer1,
            RollbackFrames = online.RollbackFrames,
        };
        try
        {
            return OnlineLaunchOptions.WithTuning(settings, _args);
        }
        catch (FormatException exception)
        {
            GD.PushError($"Ignoring invalid online options: {exception.Message}");
            return settings;
        }
    }

    // The data hash goes in the description so the two players can compare it until the handshake does (P2-05).
    private static string Describe(
        PeerMatchSettings settings, OnlineOpponent opponent, MatchLineup lineup, GameData data) => string.Create(
        CultureInfo.InvariantCulture,
        $"online P{(settings.LocalIsPlayer1 ? 1 : 2)} vs {opponent.Name}, {lineup.Description}, " +
        $"data {data.Hash.ToString()[..8]}, " +
        $"rollback {settings.RollbackFrames} f, delay {settings.InputDelayFrames} f, " +
        $"sim {settings.NetworkSimulation.PingMilliseconds} ms ±{settings.NetworkSimulation.JitterMilliseconds} ms " +
        $"{settings.NetworkSimulation.PacketLossPercent} % loss");
}
