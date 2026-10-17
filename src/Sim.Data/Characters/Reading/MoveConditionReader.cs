using System.Collections.Generic;
using Sim.Characters;
using Sim.Data.Validation;
using Sim.Fighters;
using Sim.Modules;
using Sim.Rules;

namespace Sim.Data.Characters.Reading;

/// <summary>
/// Reads when an attack may start and what starting it does (ADR-0023): <c>requires</c> (flags set / clear),
/// <c>setsFlags</c>, <c>clearsFlags</c>, <c>cost</c> and <c>followUpOnly</c>. Basic moves have none of these.
/// </summary>
public static class MoveConditionReader
{
    private static readonly string[] _fields = ["requires", "setsFlags", "clearsFlags", "cost", "followUpOnly"];

    public static MoveData Read(FieldReader file, MoveCategory category, FighterFlagNames flags, MoveData move)
    {
        if (category == MoveCategory.Basic)
        {
            foreach (string field in _fields)
            {
                file.Forbid(field, "basic moves are entered by the state machine, so they have no conditions");
            }

            return move;
        }

        (int required, int forbidden) = ReadRequires(file.OptionalObject("requires"), flags);
        (int channel, int amount) = ReadCost(file.OptionalObject("cost"));
        return move with
        {
            RequiredFlags = required,
            ForbiddenFlags = forbidden,
            SetsFlags = ReadFlags(file, "setsFlags", flags),
            ClearsFlags = ReadFlags(file, "clearsFlags", flags),
            CostChannel = channel,
            CostAmount = amount,
            IsFollowUpOnly = file.OptionalBool("followUpOnly", absent: false) ? 1 : 0,
        };
    }

    private static (int Required, int Forbidden) ReadRequires(FieldReader? requires, FighterFlagNames flags)
    {
        if (requires is null)
        {
            return (0, 0);
        }

        int required = ReadFlags(requires, "flags", flags);
        int forbidden = ReadFlags(requires, "notFlags", flags);
        requires.Finish();
        if ((required & forbidden) != 0)
        {
            requires.Error("notFlags", "names a flag \"flags\" requires, so the move could never start");
        }

        return (required, forbidden);
    }

    private static (int Channel, int Amount) ReadCost(FieldReader? cost)
    {
        if (cost is null)
        {
            return (0, 0);
        }

        int channel = cost.RequiredInt("channel", 0, IMeterGauge.ChannelCount - 1);
        int amount = cost.RequiredInt("amount", 1, CharacterSchema.MaxCost);
        cost.Finish();
        return (channel, amount);
    }

    private static int ReadFlags(FieldReader owner, string field, FighterFlagNames flags)
    {
        IReadOnlyList<string?> names = owner.IdArray(field, required: false);
        int mask = 0;
        for (int i = 0; i < names.Count; i++)
        {
            if (names[i] is not { } name)
            {
                continue;
            }

            int bit = flags.MaskOf(name);
            if (bit == 0)
            {
                owner.Error($"{field}[{i}]", $"no fighter flag is called '{name}' (rules/flags.json has: " +
                    $"{string.Join(", ", flags.Names)})");
            }

            mask |= bit;
        }

        return mask;
    }
}
