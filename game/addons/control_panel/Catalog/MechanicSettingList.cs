using System;
using System.Collections.Generic;
using FightingGame.Addons.ControlPanel.Documents;
using Sim.Data.Modules;
using Sim.Modules;

namespace FightingGame.Addons.ControlPanel.Catalog;

/// <summary>
/// The Mechanics page: one group per mechanics module with a data file, its on/off switch first and then every config
/// value with the bounds its config layout declares (ADR-0011).
/// </summary>
/// <remarks>
/// Built from the registered modules, so a new module shows up without a change here; removing one removes its group.
/// The hints only describe; a module without one still gets its group.
/// </remarks>
public static class MechanicSettingList
{
    private static readonly Dictionary<string, string> _hints = new(StringComparer.Ordinal)
    {
        ["air_block"] = "Airborne fighters block by holding back, except the attack kinds the config rules out.",
        ["meter"] = "Tension-style gauge from attacking and moving forward; supers cost meter.",
        ["negative_penalty"] = "Retreating too long costs meter; moving forward wins it back.",
        ["roman_cancel"] = "Spend meter to cancel anything into neutral, with a freeze and a slowdown on the opponent.",
        ["instant_block"] = "A fresh back press just before the hit shortens blockstun.",
        ["pushback_guard"] = "Faultless-Defense style: hold the guard buttons while blocking to push the attacker away.",
        ["burst"] = "Combo breaker on its own gauge.",
        ["gatlings"] = "Cancels through the character data's cancel windows. Off, attacks only link.",
        ["combo_limits"] = "Damage proration, hitstun decay, gravity scaling and the OTG limit as combos get longer.",
    };

    /// <param name="exists">Whether a data-relative file exists; modules without a file (test-only) are left out.</param>
    public static IReadOnlyList<SettingGroup> For(Func<string, bool> exists)
    {
        var groups = new List<SettingGroup>();
        ModuleCatalog modules = RegisteredModules.Create();
        for (int i = 0; i < modules.Count; i++)
        {
            IMechanicModule module = modules[i];
            string path = ModuleLineupSchema.ModuleFile(module.Name);
            if (module.IsCharacterMechanic || !exists(path))
            {
                continue;
            }

            PanelFile file = PanelFile.InData(path);
            var settings = new List<PanelSetting>();
            for (int field = 0; field < module.ConfigLayout.Count; field++)
            {
                ModuleConfigField config = module.ConfigLayout[field];
                PanelSetting setting =
                    PanelSetting.Whole(SettingNames.FromKey(config.Key), file, config.Key, config.Min, config.Max);
                settings.Add(setting with { Unit = SettingNames.UnitOfKey(config.Key), Hint = config.Hint });
            }

            PanelSetting enabled = PanelSetting.Flag("Enabled", file, ModuleLineupSchema.EnabledKey);
            groups.Add(new SettingGroup(
                SettingNames.FromId(module.Name),
                settings,
                enabled with { Hint = "Switches the mechanic on or off in every match." })
            {
                Hint = _hints.TryGetValue(module.Name, out string? hint) ? hint : string.Empty,
            });
        }

        return groups;
    }
}
