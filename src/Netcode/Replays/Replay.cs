using System.Collections.Generic;
using Netcode.Drivers;

namespace Netcode.Replays;

/// <summary>A recorded match: its header and every frame's inputs, ready for <see cref="ReplayMatchDriver{TState}"/>.</summary>
public sealed class Replay
{
    /// <param name="inputs">Every frame's inputs from the first frame on; frame n is at index n - 1.</param>
    public Replay(ReplayHeader header, IReadOnlyList<FrameInputs> inputs)
    {
        Header = header;
        Inputs = inputs;
    }

    public ReplayHeader Header { get; }

    /// <summary>Every frame's inputs from the first frame on, as the replay driver takes them.</summary>
    public IReadOnlyList<FrameInputs> Inputs { get; }
}
