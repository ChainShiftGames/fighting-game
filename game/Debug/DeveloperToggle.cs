using Godot;

namespace FightingGame.Debug;

/// <summary>An on/off developer setting (ARCHITECTURE.md §6) with its own key, or <c>Key.None</c> for none; the debug panel lists it.</summary>
/// <remarks>Lives only while the game runs: toggles are for looking at the match, not for changing its rules.</remarks>
public sealed class DeveloperToggle
{
    public DeveloperToggle(string name, Key key, bool enabled = false)
    {
        Name = name;
        Key = key;
        Enabled = enabled;
    }

    public string Name { get; }

    public Key Key { get; }

    public bool Enabled { get; set; }

    public void Flip() => Enabled = !Enabled;
}
