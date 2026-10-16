using Sim.Characters;
using Sim.Data.Validation;

namespace Sim.Data.Characters.Reading;

/// <summary>Reads counter hit and punish counter bonuses (ADR-0017), in hit data and in the match rules.</summary>
public static class CounterBonusReader
{
    /// <summary>A hit's own bonus object (<c>counterHit</c>, <c>punishCounter</c>); not set when it's left out.</summary>
    public static CounterBonus ReadOwn(FieldReader hit, string name)
    {
        FieldReader? bonus = hit.OptionalObject(name);
        if (bonus is null)
        {
            return default;
        }

        CounterBonus own = Read(bonus, prefix: "") with { IsSet = 1 };
        bonus.Finish();
        return own;
    }

    /// <summary>
    /// The three bonus fields, all required: <c>extraHitstun</c>, <c>extraHitstop</c> and <c>extraDamagePercent</c>,
    /// after <paramref name="prefix"/> in camelCase (<c>counterHitExtraHitstun</c>) when it isn't empty.
    /// </summary>
    public static CounterBonus Read(FieldReader fields, string prefix) => new()
    {
        ExtraHitstun = fields.RequiredInt(NameOf(prefix, "Hitstun"), 0, CharacterSchema.MaxMoveFrames),
        ExtraHitstop = fields.RequiredInt(NameOf(prefix, "Hitstop"), 0, CharacterSchema.MaxMoveFrames),
        ExtraDamagePercent = fields.RequiredInt(
            NameOf(prefix, "DamagePercent"), 0, CharacterSchema.MaxExtraDamagePercent),
    };

    private static string NameOf(string prefix, string field) =>
        prefix.Length == 0 ? $"extra{field}" : $"{prefix}Extra{field}";
}
