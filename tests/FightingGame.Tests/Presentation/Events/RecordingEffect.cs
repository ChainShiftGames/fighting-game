using System.Collections.Generic;
using FightingGame.Presentation.Events;
using Sim.Events;

namespace FightingGame.Tests.Presentation.Events;

/// <summary>Remembers every play and cancel it receives, in order.</summary>
internal sealed class RecordingEffect : IFrameEventEffect
{
    public List<FrameEvent> Played { get; } = [];

    public List<FrameEvent> Cancelled { get; } = [];

    public void Play(in FrameEvent frameEvent) => Played.Add(frameEvent);

    public void Cancel(in FrameEvent frameEvent) => Cancelled.Add(frameEvent);
}
