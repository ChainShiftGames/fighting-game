using Sim.Characters;
using Sim.State;

namespace FightingGame.Presentation.Hud;

/// <summary>
/// Turns the game state into what the HUD shows for each player, keeping the damage trails and finished combo counts
/// between frames.
/// </summary>
public sealed class HudReadout
{
    private readonly CharacterData[] _characters;
    private readonly HudGauges _gauges;
    private readonly DamageTrail[] _trails = [new(), new()];
    private readonly ComboCountHold[] _combos = [new(), new()];

    /// <param name="characters">P1's and P2's character, for their max health.</param>
    public HudReadout(CharacterData[] characters, HudGauges gauges)
    {
        _characters = characters;
        _gauges = gauges;
    }

    public HudGauges Gauges => _gauges;

    /// <summary>Call once per displayed frame for each player, P1 first; it moves that player's damage trail on.</summary>
    public HudPlayerValues Read(in MatchState state, int player)
    {
        ref readonly FighterState fighter = ref state.Fighters[player];
        float health = HudText.HealthFraction(fighter.Health, _characters[player].Stats.Health);
        DamageTrail trail = _trails[player];
        trail.Follow(state.Frame, health, isHeld: fighter.StunFramesLeft > 0);

        // The counter shows on the attacker's side: the hits the other fighter is taking (a true combo, ComboEndStage).
        int opponent = 1 - player;
        int comboHits = _combos[player].Follow(state.Frame, state.Fighters[opponent].ComboHits);
        return new HudPlayerValues(
            health,
            trail.Fraction,
            _gauges.BurstFraction(in state, player),
            _gauges.Meter(in state, player),
            state.Round.Wins[player],
            comboHits);
    }
}
