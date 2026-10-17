using Godot;

namespace FightingGame.Training;

/// <summary>One line of the training menu: the key that works it, what it shows and what pressing the key does.</summary>
/// <remarks>
/// Training features (frame meter P3-31, record slots P3-32, savestates P3-33) add their own entries in
/// <see cref="TrainingMenuEntries"/> instead of changing the menu.
/// </remarks>
public interface ITrainingMenuEntry
{
    Key Key { get; }

    /// <summary>The line's text without the key, e.g. "Dummy: Block all".</summary>
    string Describe();

    void Press();
}
