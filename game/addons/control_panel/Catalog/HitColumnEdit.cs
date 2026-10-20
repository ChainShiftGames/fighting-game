using System.Collections.Generic;
using FightingGame.Addons.ControlPanel.Documents;

namespace FightingGame.Addons.ControlPanel.Catalog;

/// <summary>
/// Changes one column of the Hit feel table for many hits at once (e.g. all hitstop +2, all hitstun to 90 %), so the
/// whole game's feel can be shifted in one step. Each value is rounded and kept in range like a typed one.
/// </summary>
public static class HitColumnEdit
{
    /// <summary>Multiplies the column by <paramref name="percent"/> / 100 in every row that has the field.</summary>
    public static void Scale(PanelDocument document, IEnumerable<HitRow> rows, HitColumn column, double percent) =>
        document.SetNumbers(Values(document, rows, column, value => value * percent / 100.0));

    /// <summary>Adds <paramref name="amount"/> (negative to take away) in every row that has the field.</summary>
    public static void Add(PanelDocument document, IEnumerable<HitRow> rows, HitColumn column, double amount) =>
        document.SetNumbers(Values(document, rows, column, value => value + amount));

    private static List<(PanelSetting, double)> Values(
        PanelDocument document, IEnumerable<HitRow> rows, HitColumn column, System.Func<double, double> change)
    {
        var values = new List<(PanelSetting, double)>();
        foreach (HitRow row in rows)
        {
            PanelSetting setting = column.For(row);
            if (document.Has(setting))
            {
                values.Add((setting, change(document.Number(setting))));
            }
        }

        return values;
    }
}
