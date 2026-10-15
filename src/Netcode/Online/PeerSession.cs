using System.Runtime.CompilerServices;
using Backdash;
using Backdash.Core;
using Backdash.Network.Client;
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

    /// <summary>A match driver for an online match against one other PC.</summary>
    public static IMatchDriver<TState> CreateDriver<TState>(
        IMatchSimulation<TState> simulation, PeerMatchSettings settings, PeerTransport transport)
        where TState : unmanaged =>
        new RollbackMatchDriver<TState>(simulation, Build<TState>(settings, transport));

    public static INetcodeSession<InputWord> Build<TState>(PeerMatchSettings settings, PeerTransport transport)
        where TState : unmanaged
    {
        NetcodePlayer local = NetcodePlayer.CreateLocal();
        NetcodePlayer remote = NetcodePlayer.CreateRemote(settings.RemoteEndPoint);
        NetcodePlayer[] playersInPortOrder = settings.LocalIsPlayer1 ? [local, remote] : [remote, local];
        IPeerSocketFactory sockets = settings.NetworkSimulation.IsNone
            ? transport.Sockets
            : new LagSimulatingSocketFactory(transport.Sockets, settings.NetworkSimulation);

        return RollbackNetcode
            .WithInputType(type => type.Struct<InputWord>())
            .WithPlayers(playersInPortOrder)
            .WithPort(settings.LocalPort)
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
}
