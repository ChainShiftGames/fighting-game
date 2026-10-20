using FightingGame.Presentation.Feel;
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
    private readonly IRumbleMotor _motor;
    private readonly RumbleSettings _settings;
    private readonly FrameEventId?[] _pulseOf = new FrameEventId?[MatchState.PlayerCount];

    /// <param name="settings">Pulse strengths and lengths (developer settings, presentation).</param>
    public ControllerRumble(IRumbleMotor motor, RumbleSettings settings)
    {
        _motor = motor;
        _settings = settings;
    }

    public void Play(in FrameEvent frameEvent)
    {
        int fighter = frameEvent.Fighter;
        (float heavy, float light) = (_settings.HeavyStrength, _settings.LightStrength);
        switch (frameEvent.Kind)
        {
            case FrameEventKind.Hit or FrameEventKind.ProjectileHit or FrameEventKind.ThrowHit:
                Pulse(OpponentOf(fighter), in frameEvent, heavy, _settings.HitSeconds);
                Pulse(fighter, in frameEvent, light, _settings.HitSeconds);
                break;
            case FrameEventKind.Block or FrameEventKind.ProjectileBlock:
                Pulse(OpponentOf(fighter), in frameEvent, light, _settings.HitSeconds);
                break;
            case FrameEventKind.KnockOut:
                Pulse(fighter, in frameEvent, heavy, _settings.KnockOutSeconds);
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
