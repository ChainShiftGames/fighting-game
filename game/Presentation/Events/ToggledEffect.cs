using FightingGame.Debug;
using Sim.Events;

namespace FightingGame.Presentation.Events;

/// <summary>Starts an effect only while its developer toggle is on (ARCHITECTURE.md §6, §9).</summary>
/// <remarks>Cancels always pass: an effect started before the toggle went off still stops if rollback removes it.</remarks>
public sealed class ToggledEffect : IFrameEventEffect
{
    private readonly DeveloperToggle _toggle;
    private readonly IFrameEventEffect _effect;

    public ToggledEffect(DeveloperToggle toggle, IFrameEventEffect effect)
    {
        _toggle = toggle;
        _effect = effect;
    }

    public void Play(in FrameEvent frameEvent)
    {
        if (_toggle.Enabled)
        {
            _effect.Play(in frameEvent);
        }
    }

    public void Cancel(in FrameEvent frameEvent) => _effect.Cancel(in frameEvent);
}
