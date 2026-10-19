using Godot;

namespace FightingGame.Presentation.Hud;

/// <summary>How one kind of glass gauge looks; sizes in reference pixels (<see cref="HudLayout"/>).</summary>
/// <param name="Start">The fill colour at the left end on P1's side (mirrored for P2).</param>
/// <param name="Middle">The fill colour halfway.</param>
/// <param name="End">The fill colour at the right end on P1's side.</param>
/// <param name="Glow">The glow around the fill; alpha is its strength.</param>
/// <param name="Trail">The damage trail behind the fill; transparent for a gauge without one.</param>
/// <param name="Inset">Gap between the glass edge and the fill.</param>
/// <param name="GlowSize">How far the glow reaches.</param>
public readonly record struct GaugeLook(
    Color Start,
    Color Middle,
    Color End,
    Color Glow,
    Color Trail,
    float Inset,
    float GlowSize);
