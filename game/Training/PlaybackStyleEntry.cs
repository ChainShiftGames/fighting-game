using System;
using Godot;
using Netcode.Drivers.Training;

namespace FightingGame.Training;

/// <summary>The menu line that picks how the dummy plays recordings back: each press moves to the next style.</summary>
public sealed class PlaybackStyleEntry : ITrainingMenuEntry
{
    private static readonly PlaybackStyle[] _styles = Enum.GetValues<PlaybackStyle>();

    private readonly CurrentTrainingSettings _settings;

    public PlaybackStyleEntry(Key key, CurrentTrainingSettings settings)
    {
        Key = key;
        _settings = settings;
    }

    public Key Key { get; }

    public string Describe() => $"Playback: {_settings.Value.Playback}";

    public void Press()
    {
        PlaybackStyle next = _styles[(Array.IndexOf(_styles, _settings.Value.Playback) + 1) % _styles.Length];
        _settings.Change(_settings.Value with { Playback = next });
    }
}
