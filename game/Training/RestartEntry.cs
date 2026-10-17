using System;
using Godot;

namespace FightingGame.Training;

/// <summary>The menu line that puts both fighters back at their start positions with full health.</summary>
public sealed class RestartEntry : ITrainingMenuEntry
{
    private readonly Action _restart;

    public RestartEntry(Key key, Action restart)
    {
        Key = key;
        _restart = restart;
    }

    public Key Key { get; }

    public string Describe() => "Reset positions";

    public void Press() => _restart();
}
