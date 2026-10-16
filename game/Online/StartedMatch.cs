using System;
using FightingGame.Diagnostics;
using FightingGame.Loop;
using Godot;

namespace FightingGame.Online;

/// <summary>
/// A match ready to play: its loop, the node that draws it, a one-line description for the overlay and, for online
/// matches, the connection meter.
/// </summary>
public sealed class StartedMatch : IDisposable
{
    private readonly IDisposable _driver;

    public StartedMatch(IMatchLoop loop, IDisposable driver, Node2D view, string description, ConnectionMeter? connection)
    {
        Loop = loop;
        _driver = driver;
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

    public void Dispose()
    {
        _driver.Dispose();
        View.QueueFree();
    }
}
