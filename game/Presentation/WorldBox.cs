using Sim.Characters;
using Sim.Math;

namespace FightingGame.Presentation;

/// <summary>One of a fighter's boxes on the current frame, placed in the world (facing and position applied).</summary>
public readonly record struct WorldBox(BoxKind Kind, AxisAlignedBox Area);
