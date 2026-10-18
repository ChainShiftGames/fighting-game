using System.Collections.Generic;
using FightingGame.VfxLab.Files;

namespace FightingGame.VfxLab;

/// <summary>The timing sliders the Lab offers, and timing with the devs' changes applied.</summary>
public static class LabTimingSettings
{
    public static IReadOnlyList<LabTimingSetting> All { get; } =
    [
        new("chargeFrames", "Startup frames", 0f, 60f, 1f, t => t.ChargeFrames, (t, v) => t with { ChargeFrames = v }),
        new("flightFrames", "Flight frames", 1f, 180f, 1f, t => t.FlightFrames, (t, v) => t with { FlightFrames = v }),
        new("impactFrames", "Impact frames", 1f, 90f, 1f, t => t.ImpactFrames, (t, v) => t with { ImpactFrames = v }),
        new("restFrames", "Rest frames", 0f, 90f, 1f, t => t.RestFrames, (t, v) => t with { RestFrames = v }),
        new("speed", "Speed (px/frame)", 0f, 30f, 0.5f, t => t.Speed, (t, v) => t with { Speed = v }),
        new("spawnX", "Spawn ahead (px)", 0f, 200f, 1f, t => t.SpawnX, (t, v) => t with { SpawnX = v }),
        new("spawnY", "Spawn height (px)", 0f, 250f, 1f, t => t.SpawnY, (t, v) => t with { SpawnY = v }),
        new("hitboxSize", "Hitbox (px)", 4f, 200f, 1f, t => t.HitboxSize, (t, v) => t with { HitboxSize = v }),
    ];

    /// <summary><paramref name="timing"/> with every change in <paramref name="changes"/> (by key) applied.</summary>
    public static LabTiming Apply(LabTiming timing, IReadOnlyDictionary<string, float> changes)
    {
        foreach (LabTimingSetting setting in All)
        {
            if (changes.TryGetValue(setting.Key, out float value))
            {
                timing = setting.With(timing, value);
            }
        }

        return timing;
    }
}
