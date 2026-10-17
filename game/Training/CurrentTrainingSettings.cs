using System;

namespace FightingGame.Training;

/// <summary>The training settings as they are now: the menu changes them, the running training match reads them every frame.</summary>
public sealed class CurrentTrainingSettings
{
    private readonly Action<TrainingSettings> _save;

    /// <param name="loaded">The settings saved last time.</param>
    /// <param name="save">Keeps a changed copy for next time.</param>
    public CurrentTrainingSettings(TrainingSettings loaded, Action<TrainingSettings> save)
    {
        Value = loaded;
        _save = save;
    }

    public TrainingSettings Value { get; private set; }

    /// <summary>Applies at once (from the next logic frame) and saves.</summary>
    public void Change(TrainingSettings settings)
    {
        Value = settings;
        _save(settings);
    }
}
