using Godot;

namespace FightingGame.Training;

/// <summary>The menu line that shows or hides the frame meter (a player setting).</summary>
public sealed class FrameMeterEntry : ITrainingMenuEntry
{
    private readonly CurrentTrainingSettings _settings;

    public FrameMeterEntry(Key key, CurrentTrainingSettings settings)
    {
        Key = key;
        _settings = settings;
    }

    public Key Key { get; }

    public string Describe() => $"Frame meter: {(_settings.Value.ShowFrameMeter ? "On" : "Off")}";

    public void Press() => _settings.Change(_settings.Value with { ShowFrameMeter = !_settings.Value.ShowFrameMeter });
}
