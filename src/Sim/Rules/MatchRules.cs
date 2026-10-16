using Sim.Characters;
using Sim.State;

namespace Sim.Rules;

/// <summary>Every gameplay-affecting setting of a match, as one blittable struct (developer settings, ADR-0011).</summary>
/// <remarks>
/// The logic reads gameplay settings only from here. Players never change these: they are fixed in shipped builds and
/// compared by <see cref="ComputeHash"/> before an online match (ARCHITECTURE.md §6). Fields are 4-byte integers or
/// structs of them (booleans as 0/1, enums backed by int), so the struct has no padding and equal rules hash equally;
/// a test checks that and changes every field in turn. Later tasks add their settings as fields here.
/// </remarks>
public struct MatchRules
{
    /// <summary>The enabled mechanics modules in hook order, with each one's config.</summary>
    public ModuleSlots Modules;

    /// <summary>Frames a motion (236, 623…) may take, first direction to button press (ADR-0002; 12 by default).</summary>
    /// <remarks>Developer setting. A motion in data may set its own window instead.</remarks>
    public int MotionWindow;

    /// <summary>Frames a button press stays valid after it happened (ADR-0002; 2 by default).</summary>
    /// <remarks>Developer setting. A press 2 frames ago still counts with a buffer of 2; 3 frames ago doesn't.</remarks>
    public int ButtonBuffer;

    /// <summary>Each fighter's distance from the stage centre when a round starts, in subpixels.</summary>
    /// <remarks>Developer setting, from data/rules/match.json.</remarks>
    public int RoundStartDistance;

    /// <summary>Each stage wall's distance from the stage centre, in subpixels; fighters' pushboxes stay inside.</summary>
    /// <remarks>Developer setting, from data/rules/match.json.</remarks>
    public int WallDistance;

    /// <summary>The farthest two fighters may stand apart, outer pushbox edge to outer pushbox edge, in subpixels.</summary>
    /// <remarks>
    /// Developer setting, from data/rules/match.json. It is the width the logic camera sees (ADR-0004): the screen
    /// edges hold fighters like walls. At most twice <see cref="WallDistance"/>.
    /// </remarks>
    public int MaxPlayerDistance;

    /// <summary>What happens when both fighters' attacks connect on the same frame.</summary>
    /// <remarks>Developer setting, from data/rules/match.json (<c>tradeRule</c>).</remarks>
    public TradeRule TradeRule;

    /// <summary>Round timer length in frames (99 s = 5940); the round times out when it runs down.</summary>
    /// <remarks>
    /// Developer setting, from data/rules/match.json (<c>roundTimerFrames</c>). Data requires a limit; 0 (rules built
    /// in code) means no time limit.
    /// </remarks>
    public int RoundTimerFrames;

    /// <summary>Round wins a player needs to win the match (2 = best of 3).</summary>
    /// <remarks>Developer setting, from data/rules/match.json (<c>roundsToWin</c>).</remarks>
    public int RoundsToWin;

    /// <summary>The last round a match can reach; still undecided after it, the match is a draw.</summary>
    /// <remarks>
    /// Developer setting, from data/rules/match.json (<c>maxRounds</c>); at least 2 × <see cref="RoundsToWin"/> − 1, so
    /// only extra deciding rounds after draws can reach it (<c>Sim.Match.MatchScore</c>).
    /// </remarks>
    public int MaxRounds;

    /// <summary>Frames between a KO or time-out and the next round (or match over), with neutral input.</summary>
    /// <remarks>Developer setting, from data/rules/match.json (<c>roundOverFrames</c>).</remarks>
    public int RoundOverFrames;

    /// <summary>Entity pool slots per player: projectiles and module objects one player can have out at once.</summary>
    /// <remarks>
    /// Developer setting, from data/rules/match.json (<c>entitiesPerPlayer</c>); at most
    /// <see cref="EntityPool.MaxPerPlayer"/>, the state's fixed layout. 0 (rules built in code) means no entities.
    /// </remarks>
    public int EntitiesPerPlayer;

    /// <summary>What a counter hit adds to a hit that has no counter hit bonus of its own (ADR-0017).</summary>
    /// <remarks>Developer setting, from data/rules/match.json (<c>counterHit*</c>); its <c>IsSet</c> is unused.</remarks>
    public CounterBonus CounterHit;

    /// <summary>What a punish counter adds to a hit that has no punish counter bonus of its own (ADR-0017).</summary>
    /// <remarks>Developer setting, from data/rules/match.json (<c>punishCounter*</c>); its <c>IsSet</c> is unused.</remarks>
    public CounterBonus PunishCounter;

    /// <summary>Hash over the whole struct, so no gameplay setting can be left out of the online check.</summary>
    public readonly ulong ComputeHash() => StateChecksum.Compute(in this);
}
