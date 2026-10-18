namespace FightingGame.VfxLab;

/// <summary>What part of the cast the Lab's timeline is on.</summary>
public enum LabPhase
{
    Charge,
    Flight,
    Impact,

    /// <summary>The empty frames after the impact, before the loop starts again.</summary>
    Rest,
}
