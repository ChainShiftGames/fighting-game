using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Sim.Analyzers.Tests;

/// <summary>Typical integer game logic must pass untouched, so the bans don't get in the way of real code.</summary>
public sealed class AllowedCodeTests
{
    [Fact]
    public async Task IntegerGameLogicHasNoDiagnostics()
    {
        string source = """
            using System;

            namespace Sim;

            public struct Fighter
            {
                public int PositionX;
                public int VelocityX;
                public int Hitstun;
            }

            public sealed class Movement
            {
                private const int SubpixelsPerPixel = 1000;
                private static readonly int[] _walkSpeeds = [3 * SubpixelsPerPixel, 4 * SubpixelsPerPixel];

                public void Tick(Span<Fighter> fighters, int frame)
                {
                    for (int i = 0; i < fighters.Length; i++)
                    {
                        ref Fighter fighter = ref fighters[i];
                        fighter.Hitstun = Math.Max(0, fighter.Hitstun - 1);
                        fighter.VelocityX = _walkSpeeds[frame % _walkSpeeds.Length];
                        fighter.PositionX = Math.Clamp(fighter.PositionX + fighter.VelocityX, -500_000, 500_000);
                    }

                    foreach (Fighter fighter in fighters)
                    {
                        _ = Math.Abs(fighter.PositionX) / SubpixelsPerPixel;
                    }
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await SimAnalysis.AnalyzeAsync(source);

        Assert.Empty(diagnostics);
    }
}
