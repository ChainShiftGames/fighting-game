namespace Netcode.Bots;

/// <summary>What a fighter looks like it is doing, as a player reads it from the animation.</summary>
/// <remarks>
/// Coarser than the logic's own action on purpose: a player sees "walking" or "in hitstun", not how many stun frames
/// are left or which input started it.
/// </remarks>
public enum FighterPose
{
    Standing,
    Crouching,
    Walking,

    /// <summary>Prejump, in the air, or air dashing.</summary>
    Jumping,
    Landing,

    /// <summary>A ground dash or backdash.</summary>
    Dashing,

    /// <summary>A normal, special, super or throw; <see cref="FighterView.AttackPhase"/> says where in it.</summary>
    Attacking,

    /// <summary>Hit: standing, crouching or in the air.</summary>
    Hitstun,

    /// <summary>Blocking: standing, crouching or in the air.</summary>
    Blockstun,

    /// <summary>On the floor or getting up.</summary>
    KnockedDown,

    /// <summary>Held by a throw, or separating from a teched one.</summary>
    Thrown,
}
