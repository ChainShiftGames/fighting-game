using System;
using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Validation;

namespace Sim.Data.Characters.Reading;

/// <summary>Reads a move's <c>cancels</c>: windows, outcomes and targets (move ids, whole categories or a jump).</summary>
public sealed class MoveCancelReader
{
    private readonly IReadOnlyList<string> _moveIds;

    public MoveCancelReader(IReadOnlyList<string> moveIds)
    {
        _moveIds = moveIds;
    }

    public IReadOnlyList<CancelRecord> Read(FieldReader move, MoveFrames frames)
    {
        List<CancelRecord> cancels = [];
        foreach (FieldReader? cancel in move.ObjectArray("cancels", required: false))
        {
            if (cancel is not null)
            {
                cancels.Add(ReadCancel(cancel, frames));
            }
        }

        return cancels;
    }

    private CancelRecord ReadCancel(FieldReader cancel, MoveFrames frames)
    {
        int firstFrame = cancel.RequiredInt("firstFrame", 1, frames.Total);
        int lastFrame = cancel.RequiredInt("lastFrame", firstFrame, frames.Total);
        CancelOn on = CancelOn.None;
        foreach (CancelOn outcome in cancel.ChoiceArray("on", CharacterJsonNames.CancelOutcomes, required: true))
        {
            on |= outcome;
        }

        MoveCategory categories = MoveCategory.None;
        foreach (MoveCategory category in
            cancel.ChoiceArray("intoCategories", CharacterJsonNames.CancelCategories, required: false))
        {
            categories |= category;
        }

        List<int> targets = ReadTargets(cancel);
        bool intoJump = cancel.OptionalBool("intoJump", absent: false);
        cancel.Finish();

        if (on == CancelOn.None)
        {
            cancel.Error("on", "must list at least one of \"hit\", \"block\", \"whiff\"");
        }

        if (targets.Count == 0 && categories == MoveCategory.None && !intoJump)
        {
            cancel.Error(
                "into", "a cancel needs at least one target in \"into\", \"intoCategories\" or \"intoJump\"");
        }

        CancelData data = new()
        {
            FirstFrame = firstFrame,
            LastFrame = lastFrame,
            On = on,
            IntoCategories = categories,
            JumpCancel = intoJump ? 1 : 0,
        };
        return new CancelRecord(data, targets);
    }

    private List<int> ReadTargets(FieldReader cancel)
    {
        IReadOnlyList<string?> into = cancel.IdArray("into", required: false);
        List<int> targets = [];
        for (int i = 0; i < into.Count; i++)
        {
            string? moveId = into[i];
            if (moveId is null)
            {
                continue;
            }

            int index = IndexOf(moveId);
            if (index < 0)
            {
                cancel.Error($"into[{i}]", $"move '{moveId}' is not in the character's move list");
            }
            else if (targets.Contains(index))
            {
                cancel.Error($"into[{i}]", $"move '{moveId}' is listed twice");
            }
            else
            {
                targets.Add(index);
            }
        }

        return targets;
    }

    private int IndexOf(string moveId)
    {
        for (int i = 0; i < _moveIds.Count; i++)
        {
            if (string.Equals(_moveIds[i], moveId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
