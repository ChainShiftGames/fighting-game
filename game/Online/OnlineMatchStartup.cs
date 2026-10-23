using System;
using System.Diagnostics;
using System.Globalization;
using FightingGame.Debug;
using FightingGame.Diagnostics;
using FightingGame.Input;
using FightingGame.Loop;
using FightingGame.Presentation;
using FightingGame.Presentation.Events;
using FightingGame.Presentation.Feel;
using FightingGame.Settings;
using Godot;
using Netcode.Desync;
using Netcode.Drivers;
using Netcode.Handshake;
using Netcode.Online;
using Netcode.Replays;
using Netcode.Transport;
using Sim.Data;
using Sim.Data.Online;
using Sim.State;

namespace FightingGame.Online;

/// <summary>Builds an online match of the real game logic against one other PC, over a Backdash peer session, and records it.</summary>
public sealed class OnlineMatchStartup
{
    private readonly PlayerDevicesChoice _devices;
    private readonly InputDeviceReader _reader;
    private readonly MatchLoopFactory _loops;
    private readonly DeveloperToggle _hitboxOverlay;
    private readonly EffectToggles _effectToggles;
    private readonly LiveNetworkSimulation _networkSimulation;
    private readonly string _desyncFolder;
    private readonly string[] _args;

    /// <param name="devices">Which devices the player on this PC reads (ADR-0038).</param>
    /// <param name="hitboxOverlay">Draws every box on top of the fighters (developer setting).</param>
    /// <param name="effectToggles">Switches each kind of event effect (developer settings).</param>
    /// <param name="networkSimulation">
    /// This PC's simulated network conditions, changed from the debug panel mid-match (developer settings). Starts at
    /// the command line's conditions; from then on the panel's values carry over to every later match (rematches).
    /// </param>
    /// <param name="desyncFolder">
    /// Full path of the folder desync dumps go to (developer setting; the host uses <c>user://desyncs</c>).
    /// </param>
    /// <param name="args">Command line, for the online tuning options of <see cref="OnlineLaunchOptions"/>.</param>
    public OnlineMatchStartup(
        PlayerDevicesChoice devices,
        InputDeviceReader reader,
        MatchLoopFactory loops,
        DeveloperToggle hitboxOverlay,
        EffectToggles effectToggles,
        LiveNetworkSimulation networkSimulation,
        string desyncFolder,
        string[] args)
    {
        _devices = devices;
        _reader = reader;
        _loops = loops;
        _hitboxOverlay = hitboxOverlay;
        _effectToggles = effectToggles;
        _networkSimulation = networkSimulation;
        _desyncFolder = desyncFolder;
        _args = args;
        try
        {
            networkSimulation.Current = OnlineLaunchOptions.Simulation(args);
        }
        catch (FormatException exception)
        {
            GD.PushError($"Ignoring invalid network simulation options: {exception.Message}");
        }
    }

    /// <param name="online">The rollback window and other online developer settings from data.</param>
    /// <param name="playerSettings">The player's settings, for their input delay choice.</param>
    public StartedMatch Create(GameData data, OnlineSettings online, PlayerSettings playerSettings, OnlineOpponent opponent)
    {
        PeerMatchSettings settings = Settings(online, opponent);
        InputDelaySettings delay = InputDelay(online, playerSettings);
        MatchLineup lineup = MatchLineup.From(data);

        // One player on this PC: by default on the keyboard or the first joypad.
        PlayerDevices assignment = _devices.Online();
        PlayerInputReader[] player = PlayerInputReader.All(assignment, _reader);
        var handshake = new VersionHandshake(
            new MatchIdentity(GameVersion.Current, data.Hash.ToBytes(), lineup.Simulation.Rules));
        var recorder = new ReplayRecorder(lineup.CreateReplayHeader(data));
        var effects = new MatchEffects(
            _effectToggles, new JoypadRumbleMotor(_devices.Roster, assignment, LocalPlayerOf(settings)), EffectSettingsChoice.Load());

        // The effects observe the recorded logic: both wrappers see every resimulated frame (ADR-0018 §4).
        InputDelayMatchDriver driver = PeerSession.CreateDriver(
            effects.Observe(lineup.RecordedSimulation(recorder)), settings, delay, opponent.Transport, handshake);
        var connection = new ConnectionMeter(driver, opponent.Transport.Link, Stopwatch.Frequency);
        var view = new MatchView();
        view.Show(driver, lineup.Characters, lineup.Simulation.Rules, lineup.Screen, _hitboxOverlay, effects);

        // No hold: a reconnected controller is just read again, and the toast says what happened.
        var reconnect = new ControllerDisconnectOverlay();
        reconnect.Watch(assignment, _devices.Roster, hold: null);
        view.AddChild(reconnect);

        // An online match can't pause: the other player's game keeps running.
        MatchLoop<MatchState> loop = _loops.Create(driver, player, stepping: null);
        // Frames still on predicted inputs may change, so the replay keeps only the final ones.
        var recording = new MatchRecording(
            recorder, driver, confirmed: driver, kind: settings.LocalIsPlayer1 ? "online-p1" : "online-p2");
        return new StartedMatch(
            loop, driver, view, () => Describe(settings, delay, driver, opponent, lineup), connection, recording, handshake,
            connectionStatus: driver,
            isOver: () => driver.State.Round.Phase == RoundPhase.MatchOver);
    }

    // Only this PC's player rumbles, on the first joypad their input reads; the opponent's pad is on the other PC.
    private static int[] LocalPlayerOf(PeerMatchSettings settings) => settings.LocalIsPlayer1
        ? [0, JoypadRumbleMotor.NoLocalPlayer]
        : [JoypadRumbleMotor.NoLocalPlayer, 0];

    // The command line's tuning options override the data's values for this match (exported builds can't edit data).
    private PeerMatchSettings Settings(OnlineSettings online, OnlineOpponent opponent)
    {
        var settings = new PeerMatchSettings
        {
            LocalPort = opponent.LocalPort,
            RemoteEndPoint = opponent.Address,
            LocalIsPlayer1 = opponent.LocalIsPlayer1,
            RollbackFrames = online.RollbackFrames,
            Disconnect = new DisconnectTimeouts(
                TimeSpan.FromMilliseconds(online.Disconnect.WarningAfterMilliseconds),
                TimeSpan.FromMilliseconds(online.Disconnect.EndMatchAfterMilliseconds)),

            // Only the packets this PC sends change; the game logic never sees the simulation (ARCHITECTURE.md §6).
            NetworkSimulation = NetworkSimulation.Following(_networkSimulation),

            // Always on: a desync is never hidden (ARCHITECTURE.md §8). The players send these files to the devs.
            DesyncDumps = new DesyncDumpSettings
            {
                Folder = _desyncFolder,
                HistoryFrames = online.Desync.HistoryFrames,
            },
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

    // The player's input delay choice, or --delay standing in for it; an invalid --delay leaves the player's choice.
    private InputDelaySettings InputDelay(OnlineSettings online, PlayerSettings player)
    {
        try
        {
            return OnlineLaunchOptions.InputDelay(online.AutomaticInputDelay, player.InputDelayOverrideFrames, _args);
        }
        catch (FormatException exception)
        {
            GD.PushError($"Ignoring the delay option: {exception.Message}");
            return OnlineLaunchOptions.InputDelay(online.AutomaticInputDelay, player.InputDelayOverrideFrames, []);
        }
    }

    private static string Describe(
        PeerMatchSettings settings,
        InputDelaySettings delay,
        InputDelayMatchDriver driver,
        OnlineOpponent opponent,
        MatchLineup lineup) => string.Create(
        CultureInfo.InvariantCulture,
        $"online P{(settings.LocalIsPlayer1 ? 1 : 2)} vs {opponent.Name}, {lineup.Description}, " +
        $"rollback {settings.RollbackFrames} f, delay {driver.InputDelayFrames} f " +
        $"{(delay.PlayerOverrideFrames is null ? "auto" : "fixed")}, " +
        $"sim {settings.NetworkSimulation.PingMilliseconds} ms ±{settings.NetworkSimulation.JitterMilliseconds} ms " +
        $"{settings.NetworkSimulation.PacketLossPercent} % loss");
}
