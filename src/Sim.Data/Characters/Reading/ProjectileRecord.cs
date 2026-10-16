using System.Collections.Generic;
using Sim.Characters;

namespace Sim.Data.Characters.Reading;

/// <summary>A validated projectile of a move; its box range is filled in when the character is assembled.</summary>
public sealed record ProjectileRecord(ProjectileData Projectile, IReadOnlyList<BoxData> Boxes);
