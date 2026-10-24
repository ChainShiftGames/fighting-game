using System;
using System.Collections.Generic;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Results;

/// <summary>
/// Who points at which results entry, and who wants the rematch. Each chooser (a player on this PC) has their own
/// cursor. Rematch needs everyone in versus: confirming it ticks that chooser in, back takes the tick away. Any other
/// entry one chooser confirms is chosen for both.
/// </summary>
public sealed class ResultsChoices
{
    public const int MaxChoosers = 2;

    private readonly int[] _cursors = new int[MaxChoosers];
    private readonly bool[] _isIn = new bool[MaxChoosers];
    private IReadOnlyList<ResultsEntry> _entries = [];

    public IReadOnlyList<ResultsEntry> Entries => _entries;

    public int Choosers { get; private set; }

    /// <summary>Starts over for a new results screen: every cursor on the first entry, nobody in.</summary>
    public void Begin(IReadOnlyList<ResultsEntry> entries, int choosers)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(choosers, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(choosers, MaxChoosers);
        _entries = entries;
        Choosers = choosers;
        Array.Clear(_cursors);
        Array.Clear(_isIn);
    }

    public int CursorOf(int chooser) => _cursors[chooser];

    /// <summary>Whether the chooser pressed rematch and waits for the other.</summary>
    public bool IsIn(int chooser) => chooser >= 0 && chooser < Choosers && _isIn[chooser];

    /// <summary>One chooser's press. Returns the entry chosen for everyone, or null when nothing was chosen yet.</summary>
    /// <remarks>Rematch with one chooser is chosen at once; with two it only ticks this one in.</remarks>
    public ResultsEntry? Press(int chooser, MenuAction actions)
    {
        if (chooser < 0 || chooser >= Choosers || _entries.Count == 0)
        {
            return null;
        }

        if (_isIn[chooser])
        {
            // Waiting for the other: the cursor stays on rematch, back takes the tick away.
            _isIn[chooser] = (actions & MenuAction.Back) == 0;
            return null;
        }

        if ((actions & (MenuAction.Up | MenuAction.Down)) != 0)
        {
            int step = (actions & MenuAction.Up) != 0 ? -1 : 1;
            _cursors[chooser] = Math.Clamp(_cursors[chooser] + step, 0, _entries.Count - 1);
            return null;
        }

        if ((actions & MenuAction.Confirm) == 0)
        {
            return null;
        }

        ResultsEntry entry = _entries[_cursors[chooser]];
        if (entry == ResultsEntry.Rematch && Choosers > 1)
        {
            _isIn[chooser] = true;
            return null;
        }

        return entry;
    }

    /// <summary>
    /// Everyone wants the rematch: at least one chooser pressed it and each other one did too or can't press (no
    /// connected controller, e.g. P2 when there is only the keyboard).
    /// </summary>
    /// <param name="canPress">Whether a chooser has a connected device; asked now, so a pad plugged in counts.</param>
    public bool IsRematchAgreed(Func<int, bool> canPress)
    {
        bool anyIn = false;
        for (int chooser = 0; chooser < Choosers; chooser++)
        {
            if (_isIn[chooser])
            {
                anyIn = true;
            }
            else if (canPress(chooser))
            {
                return false;
            }
        }

        return anyIn;
    }
}
