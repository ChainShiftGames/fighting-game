using Netcode.Drivers;
using Sim.Data.Online;
using Sim.State;

namespace FightingGame.Presentation.Events;

/// <summary>
/// Puts together one match's event effects (sparks, shake, sounds, rumble) behind one reconciler, so each event
/// plays once and effects a rollback removes are cancelled (ADR-0005 §3).
/// </summary>
/// <remarks>
/// The match startup wraps the driver's simulation with <see cref="Observe"/>; the view adds <see cref="Sounds"/> to
/// the scene as its child, draws <see cref="Sparks"/> and offsets its camera by <see cref="Shake"/>.
/// </remarks>
public sealed class MatchEffects : IEffectReset
{
    /// <summary>
    /// Frames whose played events are remembered: twice the deepest rollback window the data allows
    /// (<see cref="OnlineSchema.MaxRollbackFrames"/>), so no re-simulated frame is ever forgotten.
    /// </summary>
    public const int RememberedFrames = 2 * OnlineSchema.MaxRollbackFrames;

    private readonly FrameEventReconciler _reconciler;

    /// <param name="toggles">Switches each kind of effect (developer settings).</param>
    /// <param name="rumble">The controllers of the players on this PC.</param>
    public MatchEffects(EffectToggles toggles, IRumbleMotor rumble)
    {
        var effects = new EffectGroup(
            new ToggledEffect(toggles.Sparks, Sparks),
            new ToggledEffect(toggles.Shake, Shake),
            new ToggledEffect(toggles.Sounds, Sounds),
            new ToggledEffect(toggles.Rumble, new ControllerRumble(rumble)));
        _reconciler = new FrameEventReconciler(effects, RememberedFrames);
    }

    public HitSparks Sparks { get; } = new();

    public ScreenShake Shake { get; } = new();

    /// <summary>The host adds this node to the scene tree; sounds only play while it is there.</summary>
    public PlaceholderSounds Sounds { get; } = new();

    public void ResetEffects() => _reconciler.Reset();

    /// <summary>The simulation to give the match driver: <paramref name="simulation"/>, reporting every frame here.</summary>
    public IMatchSimulation<MatchState> Observe(IMatchSimulation<MatchState> simulation) =>
        new EventObservingSimulation(simulation, _reconciler);
}
