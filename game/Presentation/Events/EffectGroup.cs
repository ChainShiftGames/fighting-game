using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>Passes every play and cancel on to several effects, so one reconciler drives them all.</summary>
public sealed class EffectGroup : IFrameEventEffect
{
    private readonly IFrameEventEffect[] _effects;

    public EffectGroup(params IFrameEventEffect[] effects)
    {
        _effects = effects;
    }

    public void Play(in FrameEvent frameEvent)
    {
        foreach (IFrameEventEffect effect in _effects)
        {
            effect.Play(in frameEvent);
        }
    }

    public void Cancel(in FrameEvent frameEvent)
    {
        foreach (IFrameEventEffect effect in _effects)
        {
            effect.Cancel(in frameEvent);
        }
    }
}
