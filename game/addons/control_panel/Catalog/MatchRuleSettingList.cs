using System.Collections.Generic;
using FightingGame.Addons.ControlPanel.Documents;
using Sim.Data.Characters.Reading;
using Sim.Data.Online;
using Sim.Data.Rules;
using Sim.Input;
using Sim.State;

namespace FightingGame.Addons.ControlPanel.Catalog;

/// <summary>
/// The Match page: the match rules in <c>rules/match.json</c> (ADR-0011), the input windows and the rollback window,
/// grouped the way a dev looks for them, each with the range its Sim.Data loader accepts.
/// </summary>
/// <remarks>
/// Rules across fields (e.g. <c>maxRounds</c> against <c>roundsToWin</c>) stay with the loader, which the panel runs
/// before every save.
/// </remarks>
public static class MatchRuleSettingList
{
    private const int Pixel = 1000;
    private const int Second = 60;
    private const int MaxFrames = CharacterSchema.MaxMoveFrames;
    private const int MaxPercent = CharacterSchema.MaxExtraDamagePercent;
    private const int MaxThrowFrames = MatchRulesSchema.MaxThrowFrames;
    private const int PercentStep = 5;

    private static readonly PanelFile _rules = PanelFile.InData(MatchRulesSchema.File);
    private static readonly PanelFile _motions = PanelFile.InData("input/motions.json");
    private static readonly PanelFile _online = PanelFile.InData(OnlineSchema.File);

    public static IReadOnlyList<SettingGroup> All() =>
    [
        new SettingGroup("Rounds",
        [
            Frames("Round timer", "roundTimerFrames", 1, MatchRulesSchema.MaxRoundTimerFrames, Second,
                "Length of a round. 5940 frames = 99 seconds."),
            Rule("Rounds to win", "roundsToWin", 1, MatchRulesSchema.MaxRoundsToWin,
                "Round wins needed to win the match (2 = best of 3)."),
            Rule("Max rounds", "maxRounds", 1, MatchRulesSchema.MaxRounds,
                "The most rounds a match can last when draws add extra rounds."),
            Frames("Round over pause", "roundOverFrames", 1, MatchRulesSchema.MaxRoundOverFrames, 1,
                "Time between the knock-out and the next round."),
        ])
        {
            Hint = "How long rounds last and how many make a match.",
        },
        new SettingGroup("Stage and spacing",
        [
            Distance("Round start distance", "roundStartDistance", 0, MatchRulesSchema.MaxDistance,
                "How far from the stage centre each fighter starts a round."),
            Distance("Wall distance", "wallDistance", 1, MatchRulesSchema.MaxDistance,
                "How far each wall is from the stage centre: the stage's size."),
            Distance("Max player distance", "maxPlayerDistance", 1, 2 * MatchRulesSchema.MaxDistance,
                "How far apart the fighters can get: the width the camera shows."),
        ])
        {
            Hint = "The stage's size and where fighters stand at round start.",
        },
        new SettingGroup("Hits",
        [
            PanelSetting.Choice("Trade rule", _rules, "tradeRule", MatchRulesSchema.TradeRules.Names)
                with { Hint = "What happens when both fighters hit each other on the same frame." },
            Frames("Pushback slide", "pushbackFrames", 1, MatchRulesSchema.MaxPushbackFrames, 1,
                "Frames a hit's pushback is spread over."),
            PanelSetting.Flag("Air recovery", _rules, "airRecovery")
                with { Hint = "Airborne fighters may recover (air tech) once hitstun ends." },
            Rule("Entities per player", "entitiesPerPlayer", 1, EntityPool.MaxPerPlayer,
                "Projectiles and other spawned things each player may have at once."),
        ])
        {
            Hint = "Rules for every hit: trades, pushback, air recovery, projectile limits.",
        },
        new SettingGroup("Counter hits",
        [
            Frames("Counter hit extra hitstun", "counterHitExtraHitstun", 0, MaxFrames, 1,
                "Extra hitstun when a hit interrupts the opponent's attack: longer combos off counter hits."),
            Frames("Counter hit extra hitstop", "counterHitExtraHitstop", 0, MaxFrames, 1,
                "Extra freeze on a counter hit, so it reads as a big moment."),
            Rule("Counter hit extra damage %", "counterHitExtraDamagePercent", 0, MaxPercent,
                "Extra damage on a counter hit, as a share of the hit's damage (20 = 120 %).", PercentStep),
            Frames("Punish counter extra hitstun", "punishCounterExtraHitstun", 0, MaxFrames, 1,
                "Extra hitstun when a hit lands during the opponent's recovery."),
            Frames("Punish counter extra hitstop", "punishCounterExtraHitstop", 0, MaxFrames, 1,
                "Extra freeze on a punish counter."),
            Rule("Punish counter extra damage %", "punishCounterExtraDamagePercent", 0, MaxPercent,
                "Extra damage on a punish counter, as a share of the hit's damage.", PercentStep),
        ])
        {
            Hint = "Added on top of each hit's own data. Moves can override them per hit.",
        },
        new SettingGroup("Throws",
        [
            PanelSetting.Choice("Throw vs strike", _rules, "throwVsStrike", MatchRulesSchema.ThrowPriorities.Names)
                with { Hint = "Which wins when a throw and a strike connect on the same frame." },
            Frames("Throw tech window", "throwTechWindowFrames", 1, MaxThrowFrames, 1,
                "Time a grabbed fighter has to press throw and break free. Bigger = easier to tech."),
            Frames("Wake-up throw invulnerability", "wakeUpThrowInvulnerableFrames", 0, MaxThrowFrames, 1,
                "Frames after getting up in which a fighter can't be thrown."),
            Frames("Landing throw invulnerability", "landingThrowInvulnerableFrames", 0, MaxThrowFrames, 1,
                "Frames after landing from a jump in which a fighter can't be thrown."),
        ])
        {
            Hint = "Throw teching and when fighters can't be thrown.",
        },
        new SettingGroup("Special button shortcuts",
        [
            Rule("Shortcut damage %", "shortcutDamagePercent", 1, MatchRulesSchema.MaxShortcutDamagePercent,
                "Damage of specials done with the special button, as a share of the motion version's.", PercentStep),
            Frames("Shortcut cooldown", "shortcutCooldownFrames", 0, MatchRulesSchema.MaxShortcutCooldownFrames, 10,
                "Time before the special button can start another special. Motions are never locked."),
        ])
        {
            Hint = "ADR-0020: specials done with the special button instead of the motion.",
        },
        new SettingGroup("Input",
        [
            PanelSetting.Whole("Motion window", _motions, "motionWindow", 1, InputHistory.Capacity)
                with { Unit = SettingUnit.Frames, Hint = "Frames a motion input (236, 623…) may take." },
            PanelSetting.Whole("Button buffer", _motions, "buttonBuffer", 0, InputHistory.Capacity - 1)
                with { Unit = SettingUnit.Frames, Hint = "Frames a button press is remembered before it can act." },
        ])
        {
            Hint = "How forgiving inputs are (data/input/motions.json).",
        },
        new SettingGroup("Online",
        [
            PanelSetting.Whole("Rollback window", _online, "rollbackFrames", 1, OnlineSchema.MaxRollbackFrames)
                with { Unit = SettingUnit.Frames, Hint = "Frames the game may rewind to fix a late input." },
        ])
        {
            Hint = "Online netcode (data/netcode/online.json).",
        },
    ];

    private static PanelSetting Rule(string label, string key, int min, int max, string hint = "", int step = 1) =>
        PanelSetting.Whole(label, _rules, key, min, max, step) with { Hint = hint };

    private static PanelSetting Frames(string label, string key, int min, int max, int step = 1, string hint = "") =>
        PanelSetting.Whole(label, _rules, key, min, max, step) with { Unit = SettingUnit.Frames, Hint = hint };

    private static PanelSetting Distance(string label, string key, int min, int max, string hint) =>
        PanelSetting.Whole(label, _rules, key, min, max, Pixel) with { Unit = SettingUnit.Subpixels, Hint = hint };
}
