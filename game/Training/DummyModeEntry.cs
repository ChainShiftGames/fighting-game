using System;
using Godot;
using Netcode.Drivers.Training;

namespace FightingGame.Training;

/// <summary>The menu line that picks what the dummy does: each press moves on to the next <see cref="DummyMode"/>.</summary>
public sealed class DummyModeEntry : ITrainingMenuEntry
{
    private static readonly DummyMode[] _modes = Enum.GetValues<DummyMode>();

    private readonly CurrentTrainingSettings _settings;

    public DummyModeEntry(Key key, CurrentTrainingSettings settings)
    {
        Key = key;
        _settings = settings;
    }

    public Key Key { get; }

    public string Describe() => $"Dummy: {Label(_settings.Value.Dummy)}";

    public void Press()
    {
        DummyMode next = _modes[(Array.IndexOf(_modes, _settings.Value.Dummy) + 1) % _modes.Length];
        _settings.Change(_settings.Value with { Dummy = next });
    }

    /// <summary>What the menu calls a mode.</summary>
    public static string Label(DummyMode mode) => mode switch
    {
        DummyMode.Stand => "Stand",
        DummyMode.Crouch => "Crouch",
        DummyMode.BlockAll => "Block all",
        DummyMode.BlockAfterFirstHit => "Block after first hit",
        DummyMode.Jump => "Jump",
        DummyMode.Record => "Record (you control the dummy)",
        _ => mode.ToString(),
    };
}
