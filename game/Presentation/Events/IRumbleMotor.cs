namespace FightingGame.Presentation.Events;

/// <summary>Vibrates a player's controller on this PC; does nothing for a player without one here.</summary>
public interface IRumbleMotor
{
    /// <param name="player">0 = P1, 1 = P2.</param>
    /// <param name="weak">Weak (high-frequency) motor strength, 0 to 1.</param>
    /// <param name="strong">Strong (low-frequency) motor strength, 0 to 1.</param>
    void Start(int player, float weak, float strong, float seconds);

    void Stop(int player);
}
