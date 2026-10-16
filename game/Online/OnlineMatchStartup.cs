using System;
using System.Diagnostics;
using System.Globalization;
using FightingGame.Debug;
using FightingGame.Diagnostics;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Presentation;
using FightingGame.Presentation.Events;
using Godot;
using Netcode.Drivers;
using Netcode.Handshake;
using Netcode.Online;
using Netcode.Replays;
using Sim.Data;
using Sim.Data.Online;
using Sim.State;

namespace FightingGame.Online;

/// <summary>Builds an online match of the real game logic against one other PC, over a Backdash peer session, and records it.</summary>
public sealed class OnlineMatchStartup
{
    private readonly PlayerBindings[] _bindings;
    private readonly IDeviceState _devices;
    private readonly JoypadRoster _joypads;
    private readonly MatchLoopFactory _loops;
    private readonly DeveloperToggle _hitboxOverlay;
    private readonly EffectToggles _effectToggles;
    private readonly string[] _args;

    /// <param name="bindings">One per player on this PC (player settings).</param>
    /// <param name="hitboxOverlay">Draws every box on top of the fighters (developer setting).</param>
    /// <param name="effectToggles">Switches each kind of event effect (developer settings).</param>
    /// <param name="args">Command line, for the online tuning options of <see cref="OnlineLaunchOptions"/>.</param>
    public OnlineMatchStartup(
        PlayerBindings[] bindings,
        IDeviceState devices,
        JoypadRoster joypads,
        MatchLoopFactory loops,
        DeveloperToggle hitboxOverlay,
        EffectToggles effectToggles,
        string[] args)
    {
        _bindings = bindings;
        _devices = devices;
        _joypads = joypads;
        _loops = loops;
        _hitboxOverlay = hitboxOverlay;
        _effectToggles = effectToggles;
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
        var handshake = new VersionHandshake(
            new MatchIdentity(GameVersion.Current, data.Hash.ToBytes(), lineup.Simulation.Rules));
        var recorder = new ReplayRecorder(lineup.CreateReplayHeader(data));
        var effects = new MatchEffects(_effectToggles, new JoypadRumbleMotor(_joypads, RumbleSlots(settings)));

        // The effects observe the recorded logic: both wrappers see every resimulated frame (ADR-0018 §4).
        RollbackMatchDriver<MatchState> driver = PeerSession.CreateDriver(
            effects.Observe(lineup.RecordedSimulation(recorder)), settings, opponent.Transport, handshake);
        var connection = new ConnectionMeter(driver, opponent.Transport.Link, Stopwatch.Frequency);
        var view = new MatchView();
        view.Show(driver, lineup.Characters, lineup.Walls, _hitboxOverlay, effects);

        // An online match can't pause: the other player's game keeps running.
        MatchLoop<MatchState> loop = _loops.Create(driver, player, stepping: null);
        // Frames inside the rollback window may still change, so the replay keeps only the ones before it.
        var recording = new MatchRecording(
            recorder, driver, settings.RollbackFrames, kind: settings.LocalIsPlayer1 ? "online-p1" : "online-p2");
        return new StartedMatch(
            loop, driver, view, Describe(settings, opponent, lineup), connection, recording, handshake);
    }

    // Only this PC's player rumbles, on the first joypad their input reads; the opponent's pad is on the other PC.
    private static int[] RumbleSlots(PeerMatchSettings settings) => settings.LocalIsPlayer1
        ? [0, JoypadRumbleMotor.NoSlot]
        : [JoypadRumbleMotor.NoSlot, 0];

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

    private static string Describe(PeerMatchSettings settings, OnlineOpponent opponent, MatchLineup lineup) => string.Create(
        CultureInfo.InvariantCulture,
        $"online P{(settings.LocalIsPlayer1 ? 1 : 2)} vs {opponent.Name}, {lineup.Description}, " +
        $"rollback {settings.RollbackFrames} f, delay {settings.InputDelayFrames} f, " +
        $"sim {settings.NetworkSimulation.PingMilliseconds} ms ±{settings.NetworkSimulation.JitterMilliseconds} ms " +
        $"{settings.NetworkSimulation.PacketLossPercent} % loss");
}
