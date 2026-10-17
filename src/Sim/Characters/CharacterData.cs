using System;

namespace Sim.Characters;

/// <summary>
/// A character in compact, read-only form for the game logic: stats plus flat arrays of moves, boxes, cancels,
/// hits, velocities and projectiles. Created once at load; never changed during a match and not part of the game state.
/// </summary>
public sealed class CharacterData
{
    /// <summary>Marks a basic state or move role that the character doesn't define.</summary>
    public const int NoMove = -1;

    private readonly string[] _moveIds;
    private readonly MoveData[] _moves;
    private readonly BoxData[] _boxes;
    private readonly CancelData[] _cancels;
    private readonly int[] _cancelTargets;
    private readonly HitData[] _hits;
    private readonly VelocityData[] _velocities;
    private readonly int[] _basicStateMoves;
    private readonly ProjectileData[] _projectiles;
    private readonly MoveRole[] _moveRoles;

    public CharacterData(
        string id,
        string displayName,
        CharacterStats stats,
        string[] moveIds,
        MoveData[] moves,
        BoxData[] boxes,
        CancelData[] cancels,
        int[] cancelTargets,
        HitData[] hits,
        VelocityData[] velocities,
        int[] basicStateMoves,
        ProjectileData[]? projectiles = null,
        MoveRole[]? moveRoles = null)
    {
        Id = id;
        DisplayName = displayName;
        Stats = stats;
        _moveIds = moveIds;
        _moves = moves;
        _boxes = boxes;
        _cancels = cancels;
        _cancelTargets = cancelTargets;
        _hits = hits;
        _velocities = velocities;
        _basicStateMoves = basicStateMoves;
        _projectiles = projectiles ?? [];
        _moveRoles = moveRoles ?? [];
    }

    public string Id { get; }

    /// <summary>For menus only; not part of the data hash.</summary>
    public string DisplayName { get; }

    public CharacterStats Stats { get; }

    /// <summary>Moves in the order of the character file, which is also their input priority.</summary>
    public ReadOnlySpan<MoveData> Moves => _moves;

    /// <summary>Move ids by move index, for tools and debug display; the logic uses indexes.</summary>
    public ReadOnlySpan<string> MoveIds => _moveIds;

    /// <summary>Move boxes and projectile boxes; each move and projectile points at its own range.</summary>
    public ReadOnlySpan<BoxData> Boxes => _boxes;

    public ReadOnlySpan<CancelData> Cancels => _cancels;

    /// <summary>Move indexes that cancels point into.</summary>
    public ReadOnlySpan<int> CancelTargets => _cancelTargets;

    public ReadOnlySpan<HitData> Hits => _hits;

    public ReadOnlySpan<VelocityData> Velocities => _velocities;

    /// <summary>Every move's projectiles, move by move; entities refer to them by index (P1-21).</summary>
    public ReadOnlySpan<ProjectileData> Projectiles => _projectiles;

    /// <summary>Move index per <see cref="BasicState"/>, or <see cref="NoMove"/>.</summary>
    public ReadOnlySpan<int> BasicStateMoves => _basicStateMoves;

    public int BasicStateMove(BasicState state) => _basicStateMoves[(int)state];

    /// <summary>The moves the character names for module jobs (ADR-0023), in data order.</summary>
    public ReadOnlySpan<MoveRole> MoveRoles => _moveRoles;

    /// <summary>The move the character names for <paramref name="role"/>, or <see cref="NoMove"/>.</summary>
    /// <remarks>Compares strings, so the core resolves roles once at match start, never per frame.</remarks>
    public int MoveForRole(string role)
    {
        foreach (MoveRole moveRole in _moveRoles)
        {
            if (string.Equals(moveRole.Name, role, StringComparison.Ordinal))
            {
                return moveRole.Move;
            }
        }

        return NoMove;
    }

    public ReadOnlySpan<BoxData> BoxesOf(in MoveData move) => _boxes.AsSpan(move.BoxStart, move.BoxCount);

    public ReadOnlySpan<BoxData> BoxesOf(in ProjectileData projectile) =>
        _boxes.AsSpan(projectile.BoxStart, projectile.BoxCount);

    public ReadOnlySpan<CancelData> CancelsOf(in MoveData move) => _cancels.AsSpan(move.CancelStart, move.CancelCount);

    public ReadOnlySpan<int> TargetsOf(in CancelData cancel) =>
        _cancelTargets.AsSpan(cancel.TargetStart, cancel.TargetCount);

    public ReadOnlySpan<HitData> HitsOf(in MoveData move) => _hits.AsSpan(move.HitStart, move.HitCount);

    /// <summary>Index in <see cref="HitsOf"/> of the hit covering <paramref name="frame"/>; -1 when none does.</summary>
    public int HitIndexAt(in MoveData move, int frame)
    {
        ReadOnlySpan<HitData> hits = HitsOf(in move);
        for (int hit = 0; hit < hits.Length; hit++)
        {
            if (hits[hit].Covers(frame))
            {
                return hit;
            }
        }

        return -1;
    }

    /// <summary>The move's velocity ranges, in frame order and never overlapping.</summary>
    public ReadOnlySpan<VelocityData> VelocitiesOf(in MoveData move) =>
        _velocities.AsSpan(move.VelocityStart, move.VelocityCount);

    /// <summary>The projectiles the move spawns, as a range of <see cref="Projectiles"/>.</summary>
    public ReadOnlySpan<ProjectileData> ProjectilesOf(in MoveData move) =>
        _projectiles.AsSpan(move.ProjectileStart, move.ProjectileCount);
}
