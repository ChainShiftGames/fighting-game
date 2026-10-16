using System;

namespace Sim.Data.Characters.Compiled;

/// <summary>
/// A character in compact, read-only form for the game logic: stats plus flat arrays of moves, boxes, cancels and
/// hits. Created once at load; never changed during a match and not part of the game state.
/// </summary>
public sealed class CharacterData
{
    /// <summary>Marks a basic state that the character doesn't define.</summary>
    public const int NoMove = -1;

    private readonly string[] _moveIds;
    private readonly MoveData[] _moves;
    private readonly BoxData[] _boxes;
    private readonly CancelData[] _cancels;
    private readonly int[] _cancelTargets;
    private readonly HitData[] _hits;
    private readonly int[] _basicStateMoves;

    internal CharacterData(
        string id,
        string displayName,
        CharacterStats stats,
        string[] moveIds,
        MoveData[] moves,
        BoxData[] boxes,
        CancelData[] cancels,
        int[] cancelTargets,
        HitData[] hits,
        int[] basicStateMoves)
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
        _basicStateMoves = basicStateMoves;
    }

    public string Id { get; }

    /// <summary>For menus only; not part of the data hash.</summary>
    public string DisplayName { get; }

    public CharacterStats Stats { get; }

    /// <summary>Moves in the order of the character file, which is also their input priority.</summary>
    public ReadOnlySpan<MoveData> Moves => _moves;

    /// <summary>Move ids by move index, for tools and debug display; the logic uses indexes.</summary>
    public ReadOnlySpan<string> MoveIds => _moveIds;

    public ReadOnlySpan<BoxData> Boxes => _boxes;

    public ReadOnlySpan<CancelData> Cancels => _cancels;

    /// <summary>Move indexes that cancels point into.</summary>
    public ReadOnlySpan<int> CancelTargets => _cancelTargets;

    public ReadOnlySpan<HitData> Hits => _hits;

    /// <summary>Move index per <see cref="BasicState"/>, or <see cref="NoMove"/>.</summary>
    public ReadOnlySpan<int> BasicStateMoves => _basicStateMoves;

    public int BasicStateMove(BasicState state) => _basicStateMoves[(int)state];

    public ReadOnlySpan<BoxData> BoxesOf(in MoveData move) => _boxes.AsSpan(move.BoxStart, move.BoxCount);

    public ReadOnlySpan<CancelData> CancelsOf(in MoveData move) => _cancels.AsSpan(move.CancelStart, move.CancelCount);

    public ReadOnlySpan<int> TargetsOf(in CancelData cancel) =>
        _cancelTargets.AsSpan(cancel.TargetStart, cancel.TargetCount);

    public ReadOnlySpan<HitData> HitsOf(in MoveData move) => _hits.AsSpan(move.HitStart, move.HitCount);
}
