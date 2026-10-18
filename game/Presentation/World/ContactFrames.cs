namespace FightingGame.Presentation.World;

/// <summary>
/// Where an attack animation's phases start, in animation frames at 60 fps from its first key: the pose the first
/// active logic frame shows, and the pose the first recovery frame shows (ADR-0028).
/// </summary>
/// <remarks>Presentation only: they never change the logic, the data hash or a replay.</remarks>
public readonly record struct ContactFrames(int ActiveFrom, int RecoveryFrom);
