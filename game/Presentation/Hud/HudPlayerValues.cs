namespace FightingGame.Presentation.Hud;

/// <summary>What the HUD shows for one player on one frame.</summary>
public readonly record struct HudPlayerValues(
    float Health,
    float DamageTrail,
    float Burst,
    int Meter,
    int Wins,
    int ComboHits);
