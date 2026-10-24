using System;
using FightingGame.Diagnostics;
using FightingGame.Loop;
using FightingGame.Presentation;
using Godot;
using Netcode.Drivers;
using Netcode.Handshake;
using Netcode.Online;
using Sim.State;

namespace FightingGame.Online;

/// <summary>
/// A match ready to play: its loop, the node that draws it, a one-line description for the overlay, its replay
/// recording (none when playing a replay) and, for online matches, the connection meter, connection status and version
/// handshake.
/// </summary>
public sealed class StartedMatch : IDisposable
{
    private const string WaitingForOpponent = "Waiting for the other player…";

    private readonly IMatchDriver<MatchState> _driver;
    private readonly VersionHandshake? _handshake;
    private readonly IConnectionStatusReadout? _connectionStatus;
    private readonly Func<bool>? _isOver;
    private readonly Func<string> _describe;

    /// <param name="describe">The one-line description; read again each time, as some of it (the input delay) changes.</param>
    /// <param name="recording">Records the match for a replay; null when the match is a replay itself.</param>
    /// <param name="handshake">An online match's version handshake, whose refusal is shown while it waits.</param>
    /// <param name="connectionStatus">An online match's connection to the other PC: interrupted or gone.</param>
    /// <param name="isOver">Whether the match logic has finished the match; null when nothing asks.</param>
    public StartedMatch(
        IMatchLoop loop,
        IMatchDriver<MatchState> driver,
        MatchView view,
        Func<string> describe,
        ConnectionMeter? connection,
        MatchRecording? recording = null,
        VersionHandshake? handshake = null,
        IConnectionStatusReadout? connectionStatus = null,
        Func<bool>? isOver = null)
    {
        Loop = loop;
        Recording = recording;
        _driver = driver;
        _handshake = handshake;
        _connectionStatus = connectionStatus;
        _isOver = isOver;
        View = view;
        _describe = describe;
        Connection = connection;
    }

    public IMatchLoop Loop { get; }

    /// <summary>Shows the match from the driver's state; the host adds it to the scene and refreshes it each frame.</summary>
    public MatchView View { get; }

    public string Description => _describe();

    public ConnectionMeter? Connection { get; }

    /// <summary>Rounds won and, once the match is over, who won it; for the results screen. Read only (ARCHITECTURE.md §9).</summary>
    public RoundState Round => _driver.State.Round;

    public MatchRecording? Recording { get; }

    public bool IsOnline => Connection is not null;

    /// <summary>What an online match shows before its first frame: waiting, or why it will never start.</summary>
    public string WaitingMessage =>
        _handshake is { Outcome: HandshakeOutcome.Refused } refused ? refused.Refusal : WaitingForOpponent;

    /// <summary>Whether the other PC is still heard from; always connected for matches on this PC.</summary>
    public ConnectionStatus ConnectionStatus => _connectionStatus?.ReadConnectionStatus() ?? ConnectionStatus.Connected;

    /// <summary>Why the pre-match checks refused an online match; <see cref="HandshakeRefusal.None"/> otherwise.</summary>
    public HandshakeRefusal Refusal => _handshake?.Reasons ?? HandshakeRefusal.None;

    /// <summary>The other PC stopped answering for good: the match is over without a result.</summary>
    public bool OpponentDisconnected => ConnectionStatus.State == ConnectionState.Disconnected;

    /// <summary>Why the match stopped on a desync; empty while it hasn't. Shown, never hidden (ARCHITECTURE.md §8).</summary>
    public string Desync { get; private set; } = string.Empty;

    /// <summary>
    /// The match is finished, stopped (desync, disconnect), or the handshake refused it so it never starts: the lobby
    /// moves on (a rematch, or leaving when the opponent is gone).
    /// </summary>
    public bool IsOver =>
        Desync.Length > 0 || OpponentDisconnected || _handshake is { Outcome: HandshakeOutcome.Refused } ||
        (_isOver?.Invoke() ?? false);

    /// <summary>The match found a desync: it stops for good and counts as over.</summary>
    public void StopForDesync(string message) => Desync = message;

    public void Dispose()
    {
        _driver.Dispose();
        View.QueueFree();
    }
}
