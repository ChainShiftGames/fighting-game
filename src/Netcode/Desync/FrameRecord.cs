using Sim.Input;

namespace Netcode.Desync;

/// <summary>One frame as this PC ran it: both players' input words and the state they produced.</summary>
public struct FrameRecord<TState>
    where TState : unmanaged
{
    /// <summary>The frame number, from 1; 0 marks an empty slot.</summary>
    public int Frame;

    public InputWord Player1;

    public InputWord Player2;

    /// <summary>The state after the frame ran.</summary>
    public TState State;
}
