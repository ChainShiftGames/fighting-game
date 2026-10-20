using System.Collections.Generic;
using FightingGame.Addons.ControlPanel.Documents;
using Sim.Data.Characters.Reading;

namespace FightingGame.Addons.ControlPanel.Catalog;

/// <summary>
/// One field of every hit that sets how a hit feels, shown as a column of the Hit feel table, with the range
/// <c>MoveHitReader</c> accepts.
/// </summary>
public sealed record HitColumn(string Key, string Label, int Max, int Step, SettingUnit Unit, string Hint)
{
    private const int MaxFrames = CharacterSchema.MaxMoveFrames;

    public static HitColumn Damage { get; } = new(
        "damage", "Damage", CharacterSchema.MaxDamage, 50, SettingUnit.None, "Health taken on hit.");

    public static HitColumn Hitstop { get; } = new(
        "hitstop", "Hitstop", MaxFrames, 1, SettingUnit.Frames, "Frames both fighters freeze on contact: the impact.");

    public static HitColumn Hitstun { get; } = new(
        "hitstun", "Hitstun", MaxFrames, 1, SettingUnit.Frames, "Frames the defender can't act after being hit.");

    public static HitColumn Blockstun { get; } = new(
        "blockstun", "Blockstun", MaxFrames, 1, SettingUnit.Frames, "Frames the defender can't act after blocking.");

    public static HitColumn Pushback { get; } = new(
        "pushback", "Pushback", CharacterSchema.MaxDistance, 1000, SettingUnit.Subpixels,
        "How far the defender slides away.");

    /// <summary>The table's columns, in order.</summary>
    public static IReadOnlyList<HitColumn> All { get; } = [Damage, Hitstop, Hitstun, Blockstun, Pushback];

    /// <summary>This column's value in one row's hit.</summary>
    public PanelSetting For(HitRow row)
    {
        PanelSetting setting =
            PanelSetting.Whole($"{row.Label} {Label}", row.File, $"hits[{row.HitIndex}].{Key}", 0, Max, Step);
        return setting with { Unit = Unit, Hint = Hint };
    }
}
