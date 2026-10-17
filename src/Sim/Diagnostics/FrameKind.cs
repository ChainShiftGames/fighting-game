namespace Sim.Diagnostics;

/// <summary>What a fighter's frame is, as a frame meter shows it (ADR-0007): free to act, or what keeps it busy.</summary>
public enum FrameKind
{
    /// <summary>Can act: standing, crouching, walking or airborne after a jump.</summary>
    Free,

    /// <summary>An attack before its first active frame.</summary>
    Startup,

    /// <summary>An attack's active frames, from the first to the last.</summary>
    Active,

    /// <summary>An attack after its last active frame, and the landing after an air move.</summary>
    Recovery,

    /// <summary>Movement that can't act yet: prejump, dashes, backdash, air dashes, a throw tech.</summary>
    Movement,

    /// <summary>Hit: hitstun on the ground or in the air, or being thrown.</summary>
    Hitstun,

    /// <summary>Blocking: blockstun standing, crouching or in the air.</summary>
    Blockstun,

    /// <summary>Knocked down or getting up.</summary>
    Knockdown,
}
