using System.Runtime.CompilerServices;
using Backdash;
using Backdash.Core;
using Netcode.Drivers;
using Netcode.Transport;
using Sim.Input;

namespace Netcode.Online;

/// <summary>
/// Builds a Backdash peer-to-peer session between two PCs, checking every few frames that both game states still
/// match, and the rollback match driver that runs it.
/// </summary>
public static class PeerSession
{
    private const int FramesPerSecond = 60;

    // Backdash requires a non-zero port even when the transport has none (Steam relay); the socket ignores it.
    private const int PortForTransportsWithoutPorts = 1;

    /// <summary>A match driver for an online match against one other PC.</summary>
    public static RollbackMatchDriver<TState> CreateDriver<TState>(
        IMatchSimulation<TState> simulation, PeerMatchSettings settings, PeerTransport transport)
        where TState : unmanaged =>
        new RollbackMatchDriver<TState>(simulation, Build<TState>(settings, transport));

    public static INetcodeSession<InputWord> Build<TState>(PeerMatchSettings settings, PeerTransport transport)
        where TState : unmanaged
    {
        NetcodePlayer local = NetcodePlayer.CreateLocal();
        NetcodePlayer remote = NetcodePlayer.CreateRemote(settings.RemoteEndPoint);
        NetcodePlayer[] playersInPortOrder = settings.LocalIsPlayer1 ? [local, remote] : [remote, local];
        var sockets = new TransportSocketFactory(() => Open(transport, settings), settings.RemoteEndPoint, transport.Dropped);

        return RollbackNetcode
            .WithInputType(type => type.Struct<InputWord>())
            .WithPlayers(playersInPortOrder)
            .WithPort(settings.LocalPort > 0 ? settings.LocalPort : PortForTransportsWithoutPorts)
            .WithFrameRate(FramesPerSecond)
            .WithInputDelayFrames(settings.InputDelayFrames)
            .WithStateSizeHint(Unsafe.SizeOf<TState>())
            .WithConsistencyCheck(true)
            .WithSocketFactory(sockets)
            .WithLogLevel(LogLevel.Warning, appendLevel: true)
            .Configure(options => options.PredictionFrames = settings.RollbackFrames)
            .ForRemote()
            .Build();
    }

    private static ITransport Open(PeerTransport transport, PeerMatchSettings settings)
    {
        ITransport opened = transport.Transports.Open(settings.LocalPort, settings.RemoteEndPoint);
        return settings.NetworkSimulation.IsNone ? opened : new LagSimulatingTransport(opened, settings.NetworkSimulation);
    }
}
