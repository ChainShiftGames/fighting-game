using System;
using Sim.Characters;
using Sim.Math;
using Sim.State;

namespace Sim.Collision;

/// <summary>The boxes of one kind a fighter has on its current move frame, placed in the stage (world space).</summary>
/// <remarks>
/// Use with <c>foreach</c>; it is a ref struct over the character's box array, so it allocates nothing. Boxes come
/// from the playing move's data for <see cref="FighterState.MoveFrame"/>, mirrored when the fighter faces left.
/// </remarks>
public ref struct ActiveBoxes
{
    private readonly ReadOnlySpan<BoxData> _boxes;
    private readonly BoxKind _kind;
    private readonly int _frame;
    private readonly IntVector2 _position;
    private readonly int _facing;
    private int _index;

    public ActiveBoxes(CharacterData character, in FighterState fighter, BoxKind kind)
    {
        _boxes = character.BoxesOf(in character.Moves[fighter.Move]);
        _kind = kind;
        _frame = fighter.MoveFrame;
        _position = fighter.Position;
        _facing = fighter.Facing;
        _index = -1;
        Current = default;
    }

    public AxisAlignedBox Current { get; private set; }

    /// <summary>A box defined facing right, turned to <paramref name="facing"/> and moved to <paramref name="position"/>.</summary>
    public static AxisAlignedBox Place(AxisAlignedBox area, IntVector2 position, int facing) =>
        (facing < 0 ? area.MirrorX() : area).Translate(position);

    public readonly ActiveBoxes GetEnumerator() => this;

    public bool MoveNext()
    {
        while (++_index < _boxes.Length)
        {
            BoxData box = _boxes[_index];
            if (box.Kind == _kind && _frame >= box.FirstFrame && _frame <= box.LastFrame)
            {
                Current = Place(box.Area, _position, _facing);
                return true;
            }
        }

        return false;
    }
}
