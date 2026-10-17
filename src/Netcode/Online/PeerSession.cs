using System;
using System.Runtime.CompilerServices;
using Backdash;
using Backdash.Core;
using Netcode.Desync;
using Netcode.Drivers;
using Netcode.Handshake;
using Netcode.Transport;
using Sim.Input;
using Sim.State;

namespace Netcode.Online;

/// <summary>
/// Builds a Backdash peer-to-peer session between two PCs, behind the version handshake, checking every few frames
/// that both game states still match, and the rollback match driver that runs it. With desync dumps set, the driver
/// also compares every final frame with the other PC over the same transport (ADR-0019).
/// </summary>
public static class PeerSession
{
    private const int FramesPerSecond = 60;

    // Backdash requires a non-zero port even when the transport has none (Steam relay); the socket ignores it.
    private const int PortForTransportsWithoutPorts = 1;

    /// <summary>A match driver for an online match against one other PC.</summary>
    /// <param name="handshake">
    /// Checks both PCs run the same build, data and rules; the match stays at frame 0 until it agrees, and never
    /// starts when it refuses (ADR-0005 §6).
    /// </param>
    public static RollbackMatchDriver<TState> CreateDriver<TState>(
        IMatchSimulation<TState> simulation, PeerMatchSettings settings, PeerTransport transport, VersionHandshake handshake)
        where TState : unmanaged =>
        CreateRollbackDriver(simulation, settings, transport, handshake, out _);

    /// <summary>
    /// A match driver for an online match of the real game logic whose input delay follows the averaged ping between
    /// rounds, or stays at the player's choice (ADR-0005 §4).
    /// </summary>
    /// <param name="delay">Replaces <see cref="PeerMatchSettings.InputDelayFrames"/>.</param>
    /// <param name="handshake">As for the fixed-delay overload.</param>
    public static InputDelayMatchDriver CreateDriver(
        IMatchSimulation<MatchState> simulation,
        PeerMatchSettings settings,
        InputDelaySettings delay,
        PeerTransport transport,
        VersionHandshake handshake)
    {
        delay.Automatic.Validate();
        RollbackMatchDriver<MatchState> driver = CreateRollbackDriver(
            simulation,
            settings with { InputDelayFrames = delay.StartingFrames },
            transport,
            handshake,
            out INetcodeSession<InputWord> session);
        var controller = new InputDelayController(delay, driver, new SessionInputDelay(session), settings.RollbackFrames);
        return new InputDelayMatchDriver(driver, driver, driver, driver, controller);
    }

    /// <summary>A session without the per-frame desync check, for tests and tools that drive it themselves.</summary>
    /// <param name="confirmedInputs">Follows the session's confirmed frames, for its driver; none when null.</param>
    public static INetcodeSession<InputWord> Build<TState>(
        PeerMatchSettings settings,
        PeerTransport transport,
        VersionHandshake handshake,
        ConfirmedInputFrames? confirmedInputs = null)
        where TState : unmanaged =>
        Build<TState>(settings, transport, handshake, confirmedInputs, checksums: null);

    private static RollbackMatchDriver<TState> CreateRollbackDriver<TState>(
        IMatchSimulation<TState> simulation,
        PeerMatchSettings settings,
        PeerTransport transport,
        VersionHandshake handshake,
        out INetcodeSession<InputWord> session)
        where TState : unmanaged
    {
        var confirmedInputs = new ConfirmedInputFrames();
        DesyncDumpSettings? dumps = settings.DesyncDumps;
        TransportChecksumLink? checksums = dumps is null ? null : new TransportChecksumLink(dumps.HistoryFrames);
        session = Build<TState>(settings, transport, handshake, confirmedInputs, checksums);
        DesyncMonitor<TState>? monitor = dumps is null
            ? null
            : new DesyncMonitor<TState>(dumps, DumpHeader(settings, handshake), checksums!, TimeProvider.System);
        return new RollbackMatchDriver<TState>(simulation, session, clock: null, confirmedInputs, monitor);
    }

    // The same build and data the handshake compares, so a dump says exactly what the two PCs agreed to run.
    private static DesyncDumpHeader DumpHeader(PeerMatchSettings settings, VersionHandshake handshake) => new(
        handshake.Local.GameVersion, Convert.ToHexString(handshake.Local.DataHash), settings.LocalIsPlayer1);

    private static INetcodeSession<InputWord> Build<TState>(
        PeerMatchSettings settings,
        PeerTransport transport,
        VersionHandshake handshake,
        ConfirmedInputFrames? confirmedInputs,
        TransportChecksumLink? checksums)
        where TState : unmanaged
    {
        NetcodePlayer local = NetcodePlayer.CreateLocal();
        NetcodePlayer remote = NetcodePlayer.CreateRemote(settings.RemoteEndPoint);
        NetcodePlayer[] playersInPortOrder = settings.LocalIsPlayer1 ? [local, remote] : [remote, local];
        var sockets = new TransportSocketFactory(
            () => handshake.Guard(Open(transport, settings)), settings.RemoteEndPoint, transport.Dropped, checksums);

        NetcodeSessionBuilder<InputWord> builder = RollbackNetcode
            .WithInputType(type => type.Struct<InputWord>());
        if (confirmedInputs is not null)
        {
            builder = builder.WithInputListener(confirmedInputs);
        }

        return builder
            .WithPlayers(playersInPortOrder)
            .WithPort(settings.LocalPort > 0 ? settings.LocalPort : PortForTransportsWithoutPorts)
            .WithFrameRate(FramesPerSecond)
            .WithStateSizeHint(Unsafe.SizeOf<TState>())
            .WithConsistencyCheck(true)
            .WithSocketFactory(sockets)
            .WithLogLevel(LogLevel.Warning, appendLevel: true)
            .Configure(options =>
            {
                options.PredictionFrames = settings.RollbackFrames;

                // Set here: Backdash's builder rejects 0, a delay players may choose (ADR-0005 §4).
                options.InputDelayFrames = settings.InputDelayFrames;

                // A consistency check is one request and one reply, never resent, so losing them isn't a broken
                // connection; Backdash's default would disconnect after 10 s of failed checks. The checks still run,
                // and real silence still disconnects (DisconnectTimeout).
                options.Protocol.ConsistencyCheckTimeout = TimeSpan.Zero;

                // Silence from the other PC: a warning (ConnectionInterrupted), then the end (Disconnected).
                if (settings.Disconnect is { } disconnect)
                {
                    options.Protocol.DisconnectNotifyStart = disconnect.WarningAfter;
                    options.Protocol.DisconnectTimeout = disconnect.EndMatchAfter;
                }
            })
            .ForRemote()
            .Build();
    }

    private static ITransport Open(PeerTransport transport, PeerMatchSettings settings)
    {
        ITransport opened = transport.Transports.Open(settings.LocalPort, settings.RemoteEndPoint);
        return settings.NetworkSimulation.IsNone ? opened : new LagSimulatingTransport(opened, settings.NetworkSimulation);
    }
}
