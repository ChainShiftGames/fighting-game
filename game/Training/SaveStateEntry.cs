using Godot;

namespace FightingGame.Training;

/// <summary>The menu line that saves the match as it is now as the savestate.</summary>
public sealed class SaveStateEntry : ITrainingMenuEntry
{
    private readonly TrainingSession _session;

    public SaveStateEntry(Key key, TrainingSession session)
    {
        Key = key;
        _session = session;
    }

    public Key Key { get; }

    public string Describe() => _session.HasSavestate ? "Save state (replaces the saved one)" : "Save state";

    public void Press() => _session.SaveState();
}
