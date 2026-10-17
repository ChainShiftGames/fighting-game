namespace Netcode.Bots;

/// <summary>The states of the bot's state machine: what it is trying to do this frame.</summary>
public enum BotIntent
{
    /// <summary>Nothing to do: the round isn't on, or the bot can't act (hitstun, knockdown…).</summary>
    Wait,

    /// <summary>Out of reach: walk, dash, jump in or throw a projectile.</summary>
    Approach,

    /// <summary>In reach: start an attack.</summary>
    Attack,

    /// <summary>In the opponent's reach between attacks, or an attack or projectile is coming: hold back.</summary>
    Block,

    /// <summary>The opponent is stuck in recovery within reach: hit it with the fastest attack.</summary>
    Punish,
}
