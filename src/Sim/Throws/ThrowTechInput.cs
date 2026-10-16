using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Input.Motions;
using Sim.State;

namespace Sim.Throws;

/// <summary>Whether a fighter input a throw tech: the button combination of one of its character's throws.</summary>
/// <remarks>
/// Any direction techs, so a held fighter doesn't have to pick forward or back. A press counts while it is at most
/// the button buffer old (<c>MatchRules.ButtonBuffer</c>), like any move's. A character without throw moves (category
/// <c>throw</c>) can't tech. Built once per character at match start; checks never allocate.
/// </remarks>
public sealed class ThrowTechInput
{
    private readonly int[] _buttonMasks;
    private readonly int _buttonBuffer;

    public ThrowTechInput(CharacterData character, int buttonBuffer)
    {
        List<int> masks = [];
        foreach (MoveData move in character.Moves)
        {
            if (move.Category == MoveCategory.Throw && move.ButtonMask != 0 && !masks.Contains(move.ButtonMask))
            {
                masks.Add(move.ButtonMask);
            }
        }

        _buttonMasks = masks.ToArray();
        _buttonBuffer = buttonBuffer;
    }

    public bool IsTriggered(in InputHistory history)
    {
        foreach (int mask in _buttonMasks.AsSpan())
        {
            if (ButtonPress.IsPressedWithin(in history, mask, _buttonBuffer))
            {
                return true;
            }
        }

        return false;
    }
}
