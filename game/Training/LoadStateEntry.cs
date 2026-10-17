using Godot;

namespace FightingGame.Training;

/// <summary>The menu line that goes back to the savestate.</summary>
public sealed class LoadStateEntry : ITrainingMenuEntry
{
    private readonly TrainingSession _session;

    public LoadStateEntry(Key key, TrainingSession session)
    {
        Key = key;
        _session = session;
    }

    public Key Key { get; }

    public string Describe() => _session.HasSavestate ? "Load state" : "Load state (nothing saved)";

    public void Press() => _session.LoadState();
}
