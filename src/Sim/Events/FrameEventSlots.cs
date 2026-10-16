using System.Runtime.CompilerServices;

namespace Sim.Events;

/// <summary>Fixed storage for one frame's events, inline in the game state.</summary>
[InlineArray(FrameEvents.Capacity)]
public struct FrameEventSlots
{
    private FrameEvent _element;
}
