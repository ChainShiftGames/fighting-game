using System.Runtime.CompilerServices;

namespace Sim.State;

/// <summary>Fixed storage for the entity pool, inline in the match state.</summary>
[InlineArray(EntityPool.Capacity)]
public struct EntitySlots
{
    private EntityState _element;
}
