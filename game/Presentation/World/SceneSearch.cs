using Godot;

namespace FightingGame.Presentation.World;

/// <summary>Finds nodes in an instanced model scene, whose layout depends on the tool that exported it.</summary>
public static class SceneSearch
{
    /// <summary>The first <typeparamref name="T"/> at or under <paramref name="root"/>, depth first; null if none.</summary>
    public static T? First<T>(Node root)
        where T : Node
    {
        if (root is T found)
        {
            return found;
        }

        foreach (Node child in root.GetChildren())
        {
            if (First<T>(child) is { } inChild)
            {
                return inChild;
            }
        }

        return null;
    }
}
