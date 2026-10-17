namespace FightingGame.Presentation.Events;

/// <summary>Stops every running effect at once, for a jump to another point of the match (training restart, savestate).</summary>
public interface IEffectReset
{
    void ResetEffects();
}
