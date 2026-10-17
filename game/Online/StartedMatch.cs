using System;
using FightingGame.Diagnostics;
using FightingGame.Loop;
using Godot;
using Netcode.Handshake;

namespace FightingGame.Online;

/// <summary>
/// A match ready to play: its loop, the node that draws it, a one-line description for the overlay and, for online
/// matches, the connection meter and version handshake.
/// </summary>
public sealed class StartedMatch : IDisposable
{
    private const string WaitingForOpponent = "Waiting for the other player…";

    private readonly IDisposable _driver;
    private readonly VersionHandshake? _handshake;
    private readonly Func<bool>? _isOver;

    /// <param name="handshake">An online match's version handshake, whose refusal is shown while it waits.</param>
    /// <param name="isOver">Whether the match logic has finished the match; null when nothing asks.</param>
    public StartedMatch(
        IMatchLoop loop,
        IDisposable driver,
        Node2D view,
        string description,
        ConnectionMeter? connection,
        VersionHandshake? handshake = null,
        Func<bool>? isOver = null)
    {
        Loop = loop;
        _driver = driver;
        _handshake = handshake;
        _isOver = isOver;
        View = view;
        Description = description;
        Connection = connection;
    }

    public IMatchLoop Loop { get; }

    /// <summary>Draws the match from the driver's state; the host adds it to the scene and redraws it each frame.</summary>
    public Node2D View { get; }

    public string Description { get; }

    public ConnectionMeter? Connection { get; }

    public bool IsOnline => Connection is not null;

    /// <summary>What an online match shows before its first frame: waiting, or why it will never start.</summary>
    public string WaitingMessage =>
        _handshake is { Outcome: HandshakeOutcome.Refused } refused ? refused.Refusal : WaitingForOpponent;

    /// <summary>The match is finished, or the handshake refused it so it never starts: the lobby offers a rematch.</summary>
    public bool IsOver => _handshake is { Outcome: HandshakeOutcome.Refused } || (_isOver?.Invoke() ?? false);

    public void Dispose()
    {
        _driver.Dispose();
        View.QueueFree();
    }
}
