using System.Runtime.CompilerServices;
using Backdash;
using Backdash.Core;
using Netcode.Drivers;
using Netcode.Transport;
using Sim.Input;

namespace Netcode.Online;

/// <summary>
/// Builds a Backdash peer-to-peer session between two PCs, checking every few frames that both game states still
/// match. Pass it to <see cref="RollbackMatchDriver{TState}"/>.
/// </summary>
public static class PeerSession
{
    private const int FramesPerSecond = 60;

    public static INetcodeSession<InputWord> Build<TState>(PeerMatchSettings settings)
        where TState : unmanaged
    {
        NetcodePlayer local = NetcodePlayer.CreateLocal();
        NetcodePlayer remote = NetcodePlayer.CreateRemote(settings.RemoteEndPoint);
        NetcodePlayer[] playersInPortOrder = settings.LocalIsPlayer1 ? [local, remote] : [remote, local];

        return RollbackNetcode
            .WithInputType(type => type.Struct<InputWord>())
            .WithPlayers(playersInPortOrder)
            .WithPort(settings.LocalPort)
            .WithFrameRate(FramesPerSecond)
            .WithInputDelayFrames(settings.InputDelayFrames)
            .WithStateSizeHint(Unsafe.SizeOf<TState>())
            .WithConsistencyCheck(true)
            .WithSocketFactory(new UdpSocketFactory(settings.NetworkSimulation))
            .WithLogLevel(LogLevel.Warning, appendLevel: true)
            .Configure(options => options.PredictionFrames = settings.RollbackFrames)
            .ForRemote()
            .Build();
    }
}
