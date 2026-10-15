using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Backdash.Network.Client;
using Steamworks;

namespace Platform.Steam.Transport;

/// <summary>
/// Backdash's network socket over Steam Networking Messages: packets go through Steam's relay, addressed by Steam
/// ID, so neither player sees the other's IP address (ADR-0005 §1). Unreliable channel; Backdash resends itself.
/// </summary>
public sealed class SteamMessagesSocket : IPeerSocket
{
    private const int Channel = 0;
    private const int MaxMessagesPerPoll = 64;
    private const int MaxPacketSize = 1200;

    // Input packets go out at once; a lost one is resent by Backdash. Steam restarts a broken session itself.
    private const int SendFlags =
        Constants.k_nSteamNetworkingSend_UnreliableNoDelay | Constants.k_nSteamNetworkingSend_AutoRestartBrokenSession;

    private readonly ulong _opponentSteamId;
    private readonly Channel<ReceivedPacket> _received =
        System.Threading.Channels.Channel.CreateUnbounded<ReceivedPacket>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

    private readonly IntPtr[] _messages = new IntPtr[MaxMessagesPerPoll];
    private readonly byte[] _sendBuffer = GC.AllocateArray<byte>(MaxPacketSize, pinned: true);
    private readonly Lock _sendLock = new();
    private readonly Callback<SteamNetworkingMessagesSessionRequest_t> _sessionRequests;

    /// <param name="opponentSteamId">The only Steam user this socket accepts a session from.</param>
    public SteamMessagesSocket(ulong opponentSteamId)
    {
        _opponentSteamId = opponentSteamId;
        _sessionRequests = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
        SteamNetworkingUtils.InitRelayNetworkAccess();
    }

    public AddressFamily AddressFamily => AddressFamily.InterNetwork;

    public int Port => 0;

    public async ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketAddress address, CancellationToken cancellationToken)
    {
        ReceivedPacket packet = await _received.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        int length = packet.CopyTo(buffer.Span);
        SteamPeerEndPoint.WriteSteamId(address, packet.Sender);
        return length;
    }

    public async ValueTask<SocketReceiveFromResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        ReceivedPacket packet = await _received.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        int length = packet.CopyTo(buffer.Span);
        return new SocketReceiveFromResult { ReceivedBytes = length, RemoteEndPoint = new SteamPeerEndPoint(packet.Sender) };
    }

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketAddress socketAddress, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Send(buffer.Span, SteamPeerEndPoint.ReadSteamId(socketAddress)));

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, EndPoint remoteEndPoint, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Send(buffer.Span, ((SteamPeerEndPoint)remoteEndPoint).SteamId));

    // Backdash calls this on the game thread at the start of every frame, right before it reads network events,
    // so polling here hands it everything that arrived without a busy-waiting thread.
    public void Update()
    {
        int count = SteamNetworkingMessages.ReceiveMessagesOnChannel(Channel, _messages, _messages.Length);
        for (int i = 0; i < count; i++)
        {
            SteamNetworkingMessage_t message = SteamNetworkingMessage_t.FromIntPtr(_messages[i]);
            _received.Writer.TryWrite(ReceivedPacket.CopyFrom(message));
            SteamNetworkingMessage_t.Release(_messages[i]);
        }
    }

    public void Close()
    {
        _received.Writer.TryComplete();
        SteamNetworkingIdentity opponent = SteamIdentity.For(_opponentSteamId);
        SteamNetworkingMessages.CloseSessionWithUser(ref opponent);
    }

    public void Dispose()
    {
        Close();
        _sessionRequests.Dispose();
    }

    // Reports the packet as sent even when Steam refuses it: for an unreliable channel that's a lost packet.
    private int Send(ReadOnlySpan<byte> packet, ulong steamId)
    {
        SteamNetworkingIdentity recipient = SteamIdentity.For(steamId);
        lock (_sendLock)
        {
            packet.CopyTo(_sendBuffer);
            SteamNetworkingMessages.SendMessageToUser(
                ref recipient, Marshal.UnsafeAddrOfPinnedArrayElement(_sendBuffer, 0), (uint)packet.Length, SendFlags, Channel);
        }

        return packet.Length;
    }

    private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t request)
    {
        if (request.m_identityRemote.GetSteamID64() == _opponentSteamId)
        {
            SteamNetworkingMessages.AcceptSessionWithUser(ref request.m_identityRemote);
        }
    }

    private readonly record struct ReceivedPacket(byte[] Data, int Length, ulong Sender)
    {
        public static ReceivedPacket CopyFrom(SteamNetworkingMessage_t message)
        {
            byte[] data = ArrayPool<byte>.Shared.Rent(message.m_cbSize);
            Marshal.Copy(message.m_pData, data, 0, message.m_cbSize);
            return new ReceivedPacket(data, message.m_cbSize, message.m_identityPeer.GetSteamID64());
        }

        public int CopyTo(Span<byte> destination)
        {
            int length = Math.Min(Length, destination.Length);
            Data.AsSpan(0, length).CopyTo(destination);
            ArrayPool<byte>.Shared.Return(Data);
            return length;
        }
    }
}
