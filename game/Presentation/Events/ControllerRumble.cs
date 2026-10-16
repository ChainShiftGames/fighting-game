using Sim.Events;
using Sim.State;

namespace FightingGame.Presentation.Events;

/// <summary>Rumbles the controllers on contact: hard for who got hit, light for the attacker and on block.</summary>
/// <remarks>
/// Remembers which event each player's current pulse came from, so cancelling an event stops only its own pulse,
/// not a newer one.
/// </remarks>
public sealed class ControllerRumble : IFrameEventEffect
{
    // Developer settings (presentation): pulse strengths (0 to 1) and lengths.
    private const float HeavyStrength = 0.8f;
    private const float LightStrength = 0.3f;
    private const float HitSeconds = 0.15f;
    private const float KnockOutSeconds = 0.4f;

    private readonly IRumbleMotor _motor;
    private readonly FrameEventId?[] _pulseOf = new FrameEventId?[MatchState.PlayerCount];

    public ControllerRumble(IRumbleMotor motor)
    {
        _motor = motor;
    }

    public void Play(in FrameEvent frameEvent)
    {
        int fighter = frameEvent.Fighter;
        switch (frameEvent.Kind)
        {
            case FrameEventKind.Hit or FrameEventKind.ProjectileHit or FrameEventKind.ThrowHit:
                Pulse(OpponentOf(fighter), in frameEvent, HeavyStrength, HitSeconds);
                Pulse(fighter, in frameEvent, LightStrength, HitSeconds);
                break;
            case FrameEventKind.Block or FrameEventKind.ProjectileBlock:
                Pulse(OpponentOf(fighter), in frameEvent, LightStrength, HitSeconds);
                break;
            case FrameEventKind.KnockOut:
                Pulse(fighter, in frameEvent, HeavyStrength, KnockOutSeconds);
                break;
        }
    }

    public void Cancel(in FrameEvent frameEvent)
    {
        for (int player = 0; player < _pulseOf.Length; player++)
        {
            if (_pulseOf[player] == frameEvent.Id)
            {
                _motor.Stop(player);
                _pulseOf[player] = null;
            }
        }
    }

    private void Pulse(int player, in FrameEvent frameEvent, float strength, float seconds)
    {
        _motor.Start(player, weak: strength, strong: strength, seconds);
        _pulseOf[player] = frameEvent.Id;
    }

    private static int OpponentOf(int fighter) => MatchState.PlayerCount - 1 - fighter;
}
