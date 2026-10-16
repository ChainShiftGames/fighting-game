using System.Collections.Generic;
using System.Threading;
using Netcode.Transport;

namespace Netcode.Handshake;

/// <summary>
/// Agrees with the other PC on game version, game data and match rules before an online match (ADR-0005 §6). Any
/// difference refuses the match on both PCs; it never starts.
/// </summary>
/// <remarks>
/// Each PC repeats a hello carrying its identity and whether it has seen the other's. A PC agrees once the other's
/// hello matches and either says it has seen ours, or the other PC's netcode traffic arrives (it only sends that
/// after agreeing itself), so one lost hello can't leave a PC waiting forever. Read from the game thread and updated
/// from the network thread.
/// </remarks>
public sealed class VersionHandshake
{
    /// <summary>What players see when the PCs differ (ADR-0005 §6).</summary>
    public const string Mismatch = "Versions differ — update the game";

    private readonly Lock _lock = new();
    private HandshakeOutcome _outcome;
    private bool _matchedTheirs;
    private string _refusal = string.Empty;

    public VersionHandshake(MatchIdentity local)
    {
        Local = local;
    }

    public MatchIdentity Local { get; }

    public HandshakeOutcome Outcome
    {
        get
        {
            lock (_lock)
            {
                return _outcome;
            }
        }
    }

    /// <summary><see cref="Mismatch"/> and what differs, once refused; empty otherwise.</summary>
    public string Refusal
    {
        get
        {
            lock (_lock)
            {
                return _refusal;
            }
        }
    }

    /// <summary>Whether this PC's hello should tell the other PC that its hello arrived and matched.</summary>
    internal bool HasMatchedTheirs
    {
        get
        {
            lock (_lock)
            {
                return _matchedTheirs;
            }
        }
    }

    /// <summary>Wraps the transport an online match runs on, so its netcode traffic waits for agreement.</summary>
    public ITransport Guard(ITransport transport) => new HandshakeTransport(transport, this);

    internal void OnHello(MatchIdentity theirs, bool theySawOurs)
    {
        IReadOnlyList<string> differences = Local.DifferencesFrom(theirs);
        lock (_lock)
        {
            if (_outcome != HandshakeOutcome.Waiting)
            {
                return;
            }

            if (differences.Count > 0)
            {
                Refuse(string.Join("; ", differences));
                return;
            }

            _matchedTheirs = true;
            if (theySawOurs)
            {
                _outcome = HandshakeOutcome.Agreed;
            }
        }
    }

    internal void OnHelloOfAnotherFormat(int format)
    {
        lock (_lock)
        {
            if (_outcome == HandshakeOutcome.Waiting)
            {
                Refuse($"handshake format {HelloMessage.Format} here, {format} there");
            }
        }
    }

    /// <summary>The other PC's netcode traffic arrived. Returns whether it may pass on to the netcode.</summary>
    internal bool OnNetcodePacket()
    {
        lock (_lock)
        {
            if (_outcome == HandshakeOutcome.Waiting && _matchedTheirs)
            {
                _outcome = HandshakeOutcome.Agreed;
            }

            return _outcome == HandshakeOutcome.Agreed;
        }
    }

    private void Refuse(string differences)
    {
        _outcome = HandshakeOutcome.Refused;
        _refusal = $"{Mismatch}\n({differences})";
    }
}
