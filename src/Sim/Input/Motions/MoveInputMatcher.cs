using System;
using Sim.Characters;
using Sim.State;

namespace Sim.Input.Motions;

/// <summary>Decides whether one of a character's moves was input: its motion, its button and motion priority.</summary>
/// <remarks>
/// Motion priority (the <see cref="MotionSet"/> order) only compares motions the character really has a move for,
/// with the same button in the same stance: on 6236+C, 623C beats 236C, but a character without 623C still gets
/// 236C. Between moves with different buttons the character's move order decides. Built once per character at match
/// start; checks never allocate.
/// </remarks>
public sealed class MoveInputMatcher
{
    private readonly MotionParser _parser;
    private readonly MoveStance[] _groupStances;
    private readonly int[] _groupButtons;
    private readonly MotionMask[] _groupCandidates;
    private readonly int _groupCount;

    /// <exception cref="ArgumentException">A move uses a motion the motion set doesn't define.</exception>
    public MoveInputMatcher(MotionParser parser, CharacterData character)
    {
        _parser = parser;
        ReadOnlySpan<MoveData> moves = character.Moves;
        _groupStances = new MoveStance[moves.Length];
        _groupButtons = new int[moves.Length];
        _groupCandidates = new MotionMask[moves.Length];
        for (int move = 0; move < moves.Length; move++)
        {
            if (!IsInputMove(in moves[move]))
            {
                continue;
            }

            if (!parser.Motions.TryFind(moves[move].Motion, out int motion))
            {
                throw new ArgumentException(
                    $"Move '{character.MoveIds[move]}' of '{character.Id}' uses motion {moves[move].Motion}, which " +
                    "the motion set doesn't define.", nameof(character));
            }

            int group = FindGroup(moves[move].Stance, moves[move].ButtonMask);
            if (group < 0)
            {
                group = _groupCount++;
                _groupStances[group] = moves[move].Stance;
                _groupButtons[group] = moves[move].ButtonMask;
            }

            _groupCandidates[group] = _groupCandidates[group].With(motion);
        }
    }

    /// <summary>The move's motion and button were input, and no motion with higher priority was.</summary>
    /// <param name="history">The fighter's cleaned input history.</param>
    /// <param name="move">One of this character's moves.</param>
    /// <param name="facing">+1 facing right, -1 facing left.</param>
    public bool IsTriggered(in InputHistory history, in MoveData move, int facing)
    {
        if (!IsInputMove(in move) || !_parser.Motions.TryFind(move.Motion, out int motion))
        {
            return false;
        }

        int group = FindGroup(move.Stance, move.ButtonMask);
        return group >= 0 && _groupCandidates[group].Contains(motion) &&
            _parser.FindFirst(_groupCandidates[group], in history, facing, move.ButtonMask) == motion;
    }

    private static bool IsInputMove(in MoveData move) => move.Category != MoveCategory.Basic && move.ButtonMask != 0;

    private int FindGroup(MoveStance stance, int buttonMask)
    {
        for (int group = 0; group < _groupCount; group++)
        {
            if (_groupStances[group] == stance && _groupButtons[group] == buttonMask)
            {
                return group;
            }
        }

        return -1;
    }
}
