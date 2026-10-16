using System;
using Sim.Characters;
using Sim.Collision;
using Sim.Math;
using Sim.State;

namespace Sim.Entities;

/// <summary>An entity's hitboxes on its current frame, placed in the stage (world space).</summary>
/// <remarks>
/// Use with <c>foreach</c>; a ref struct over the character's box array, so it allocates nothing. Boxes come from the
/// projectile's data for <see cref="EntityState.Frame"/>, mirrored when the entity faces left, like a fighter's
/// (<see cref="ActiveBoxes"/>).
/// </remarks>
public ref struct EntityHitboxes
{
    private readonly ReadOnlySpan<BoxData> _boxes;
    private readonly int _frame;
    private readonly IntVector2 _position;
    private readonly int _facing;
    private int _index;

    public EntityHitboxes(OwnerCharacters characters, in EntityState entity)
    {
        _boxes = characters.Of(entity.Owner).BoxesOf(in characters.ProjectileOf(in entity));
        _frame = entity.Frame;
        _position = entity.Position;
        _facing = entity.Facing;
        _index = -1;
        Current = default;
    }

    public AxisAlignedBox Current { get; private set; }

    public readonly EntityHitboxes GetEnumerator() => this;

    public bool MoveNext()
    {
        while (++_index < _boxes.Length)
        {
            BoxData box = _boxes[_index];
            if (_frame >= box.FirstFrame && _frame <= box.LastFrame)
            {
                Current = ActiveBoxes.Place(box.Area, _position, _facing);
                return true;
            }
        }

        return false;
    }
}
