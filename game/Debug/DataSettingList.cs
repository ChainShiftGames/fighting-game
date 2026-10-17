using Sim.Data.Characters.Reading;
using Sim.Data.Online;
using Sim.Data.Rules;
using Sim.State;

namespace FightingGame.Debug;

/// <summary>The developer settings stored in <c>data/</c> that the debug panel lists, with Sim.Data's bounds.</summary>
/// <remarks>
/// Each range is the one the loader accepts for that field, so the panel can't write a value the loader rejects on
/// its own. Rules across fields (e.g. <c>maxRounds</c> against <c>roundsToWin</c>) stay with the loader, which
/// reports them when the match restarts.
/// </remarks>
public static class DataSettingList
{
    private const string MatchRules = MatchRulesSchema.File;
    private const string Motions = "input/motions.json";
    private const int Pixel = 1000;
    private const int Second = 60;
    private const int MaxFrames = CharacterSchema.MaxMoveFrames;
    private const int MaxPercent = CharacterSchema.MaxExtraDamagePercent;
    private const int MaxThrowFrames = MatchRulesSchema.MaxThrowFrames;

    public static DataFileSetting[] Numbers() =>
    [
        new("Round start distance", MatchRules, "roundStartDistance", 0, MatchRulesSchema.MaxDistance, Pixel),
        new("Wall distance", MatchRules, "wallDistance", 1, MatchRulesSchema.MaxDistance, Pixel),
        new("Max player distance", MatchRules, "maxPlayerDistance", 1, 2 * MatchRulesSchema.MaxDistance, Pixel),
        new("Round timer (frames)", MatchRules, "roundTimerFrames", 1, MatchRulesSchema.MaxRoundTimerFrames, Second),
        new("Rounds to win", MatchRules, "roundsToWin", 1, MatchRulesSchema.MaxRoundsToWin, 1),
        new("Max rounds", MatchRules, "maxRounds", 1, MatchRulesSchema.MaxRounds, 1),
        new("Round over pause (frames)", MatchRules, "roundOverFrames", 1, MatchRulesSchema.MaxRoundOverFrames, 1),
        new("Entities per player", MatchRules, "entitiesPerPlayer", 1, EntityPool.MaxPerPlayer, 1),
        new("Counter hit extra hitstun", MatchRules, "counterHitExtraHitstun", 0, MaxFrames, 1),
        new("Counter hit extra hitstop", MatchRules, "counterHitExtraHitstop", 0, MaxFrames, 1),
        new("Counter hit extra damage %", MatchRules, "counterHitExtraDamagePercent", 0, MaxPercent, 5),
        new("Punish counter extra hitstun", MatchRules, "punishCounterExtraHitstun", 0, MaxFrames, 1),
        new("Punish counter extra hitstop", MatchRules, "punishCounterExtraHitstop", 0, MaxFrames, 1),
        new("Punish counter extra damage %", MatchRules, "punishCounterExtraDamagePercent", 0, MaxPercent, 5),
        new("Throw tech window", MatchRules, "throwTechWindowFrames", 1, MaxThrowFrames, 1),
        new("Wake-up throw invulnerability", MatchRules, "wakeUpThrowInvulnerableFrames", 0, MaxThrowFrames, 1),
        new("Landing throw invulnerability", MatchRules, "landingThrowInvulnerableFrames", 0, MaxThrowFrames, 1),
        new("Shortcut damage %", MatchRules, "shortcutDamagePercent", 1, MatchRulesSchema.MaxShortcutDamagePercent, 5),
        new("Shortcut cooldown (frames)", MatchRules, "shortcutCooldownFrames", 0, MatchRulesSchema.MaxShortcutCooldownFrames, 10),
        new("Pushback slide (frames)", MatchRules, "pushbackFrames", 1, MatchRulesSchema.MaxPushbackFrames, 1),
        new("Motion window", Motions, "motionWindow", 1, InputHistory.Capacity, 1),
        new("Button buffer", Motions, "buttonBuffer", 0, InputHistory.Capacity - 1, 1),
        new("Rollback window (online)", OnlineSchema.File, "rollbackFrames", 1, OnlineSchema.MaxRollbackFrames, 1),
    ];

    public static DataFileChoice[] Choices() =>
    [
        new("Trade rule", MatchRules, "tradeRule", MatchRulesSchema.TradeRules.Names),
        new("Throw vs strike", MatchRules, "throwVsStrike", MatchRulesSchema.ThrowPriorities.Names),
    ];
}
