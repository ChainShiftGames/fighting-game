using System;
using System.Collections.Generic;

namespace FightingGame.UI.Framework;

/// <summary>
/// The open menu screens, newest on top; only the top one is shown and takes input. Each entry keeps its own
/// <see cref="MenuFocus"/>, so going back finds the cursor where it was left.
/// </summary>
public sealed class MenuStack
{
    private readonly List<(IMenuScreen Screen, MenuFocus Focus)> _entries = [];

    public int Count => _entries.Count;

    public bool IsEmpty => _entries.Count == 0;

    public IMenuScreen? Top => IsEmpty ? null : _entries[^1].Screen;

    /// <summary>The top screen's cursor; null while the stack is empty.</summary>
    public MenuFocus? TopFocus => IsEmpty ? null : _entries[^1].Focus;

    /// <summary>Opens a screen on top, its cursor on its first item.</summary>
    public void Push(IMenuScreen screen) => _entries.Add((screen, new MenuFocus()));

    /// <summary>Closes the top screen, showing the one under it as it was left.</summary>
    public void Pop()
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("No menu screen to close.");
        }

        _entries.RemoveAt(_entries.Count - 1);
    }

    /// <summary>Swaps the top screen for another (a step sideways, not deeper); back then skips the old one.</summary>
    public void Replace(IMenuScreen screen)
    {
        Pop();
        Push(screen);
    }

    public void Clear() => _entries.Clear();

    /// <summary>
    /// Makes the stack exactly <paramref name="path"/>, bottom first. Screens it already has in the same place keep
    /// their cursors; the rest are closed or opened. Calling it again with the same path changes nothing.
    /// </summary>
    public void Show(IReadOnlyList<IMenuScreen> path)
    {
        int kept = 0;
        while (kept < path.Count && kept < _entries.Count && ReferenceEquals(_entries[kept].Screen, path[kept]))
        {
            kept++;
        }

        _entries.RemoveRange(kept, _entries.Count - kept);
        for (int i = kept; i < path.Count; i++)
        {
            Push(path[i]);
        }
    }

    /// <summary>Back on the top screen: it decides, and is popped when it agrees and isn't the bottom one.</summary>
    public void Back()
    {
        if (Top is { } top && top.Back() && Count > 1)
        {
            Pop();
        }
    }
}
