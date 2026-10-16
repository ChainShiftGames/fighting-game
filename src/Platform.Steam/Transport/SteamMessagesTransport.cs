using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Netcode.Transport;
using Steamworks;

namespace Platform.Steam.Transport;

/// <summary>
/// Steam Networking Messages to one opponent, addressed by Steam ID. Steam routes packets directly or through its
/// relay as each player's Steam setting allows (ADR-0014). Unreliable channel; the netcode resends itself.
/// </summary>
public sealed class SteamMessagesTransport : ITransport
{
    private const int Channel = 0;
    private const int MaxMessagesPerPoll = 64;

    // Input packets go out at once; a lost one is resent by the netcode. Steam restarts a broken session itself.
    private const int SendFlags =
        Constants.k_nSteamNetworkingSend_UnreliableNoDelay | Constants.k_nSteamNetworkingSend_AutoRestartBrokenSession;

    private readonly ulong _opponentSteamId;
    private readonly Channel<ReceivedMessage> _received =
        System.Threading.Channels.Channel.CreateUnbounded<ReceivedMessage>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

    private readonly IntPtr[] _messages = new IntPtr[MaxMessagesPerPoll];
    private readonly byte[] _sendBuffer = GC.AllocateArray<byte>(ITransport.MaxPacketSize, pinned: true);
    private readonly Lock _sendLock = new();
    private readonly Callback<SteamNetworkingMessagesSessionRequest_t> _sessionRequests;
    private bool _disposed;

    /// <param name="opponentSteamId">The only Steam user this transport talks to and accepts a session from.</param>
    public SteamMessagesTransport(ulong opponentSteamId)
    {
        _opponentSteamId = opponentSteamId;
        _sessionRequests = Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
        SteamNetworkingUtils.InitRelayNetworkAccess();
    }

    // Reports nothing when Steam refuses the packet: for an unreliable channel that's a lost packet.
    public void Send(ReadOnlySpan<byte> packet)
    {
        SteamNetworkingIdentity opponent = SteamIdentity.For(_opponentSteamId);
        lock (_sendLock)
        {
            packet.CopyTo(_sendBuffer);
            SteamNetworkingMessages.SendMessageToUser(
                ref opponent, Marshal.UnsafeAddrOfPinnedArrayElement(_sendBuffer, 0), (uint)packet.Length, SendFlags, Channel);
        }
    }

    public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        ReceivedMessage message = await _received.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return message.MoveTo(buffer.Span);
    }

    // The netcode calls this on the game thread at the start of every frame, right before it reads what arrived,
    // so polling here hands it everything without a busy-waiting thread.
    public void Poll()
    {
        int count = SteamNetworkingMessages.ReceiveMessagesOnChannel(Channel, _messages, _messages.Length);
        for (int i = 0; i < count; i++)
        {
            SteamNetworkingMessage_t message = SteamNetworkingMessage_t.FromIntPtr(_messages[i]);
            if (message.m_identityPeer.GetSteamID64() == _opponentSteamId)
            {
                _received.Writer.TryWrite(ReceivedMessage.CopyFrom(message));
            }

            SteamNetworkingMessage_t.Release(_messages[i]);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _received.Writer.TryComplete();
        SteamNetworkingIdentity opponent = SteamIdentity.For(_opponentSteamId);
        SteamNetworkingMessages.CloseSessionWithUser(ref opponent);
        _sessionRequests.Dispose();
    }

    private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t request)
    {
        if (request.m_identityRemote.GetSteamID64() == _opponentSteamId)
        {
            SteamNetworkingMessages.AcceptSessionWithUser(ref request.m_identityRemote);
        }
    }

    // Keeps at most MaxPacketSize bytes of a message but its full length, so an oversized one is still reported.
    private readonly record struct ReceivedMessage(byte[] Data, int KeptLength, int Length)
    {
        public static ReceivedMessage CopyFrom(SteamNetworkingMessage_t message)
        {
            int kept = Math.Min(message.m_cbSize, ITransport.MaxPacketSize);
            byte[] data = ArrayPool<byte>.Shared.Rent(kept);
            Marshal.Copy(message.m_pData, data, 0, kept);
            return new ReceivedMessage(data, kept, message.m_cbSize);
        }

        public int MoveTo(Span<byte> destination)
        {
            Data.AsSpan(0, Math.Min(KeptLength, destination.Length)).CopyTo(destination);
            ArrayPool<byte>.Shared.Return(Data);
            return Length;
        }
    }
}
