using System;
using FightingGame.Presentation.Events;
using Godot;
using Sim.Events;

namespace FightingGame.Presentation.World.Sparks;

/// <summary>Shows the live hit sparks, each looking as its hit says (<see cref="HitSparkLooks"/>).</summary>
/// <remarks>One spark node per live spark the list can hold, made up front, so a frame never allocates.</remarks>
public partial class HitSparkViews : Node3D
{
    private readonly HitSparkArt[] _sparks = new HitSparkArt[HitSparks.MaxSparks];

    public HitSparkViews()
    {
        for (int i = 0; i < _sparks.Length; i++)
        {
            _sparks[i] = new HitSparkArt { Visible = false };
            AddChild(_sparks[i]);
        }
    }

    /// <param name="live">From <see cref="HitSparks.Live"/>.</param>
    /// <param name="drawnElsewhere">True for sparks another effect replaces (a projectile's own impact); null draws all.</param>
    public void ShowFrame(ReadOnlySpan<LiveSpark> live, HitSparkLooks looks, Func<FrameEvent, bool>? drawnElsewhere)
    {
        int used = 0;
        foreach (LiveSpark spark in live)
        {
            if (drawnElsewhere?.Invoke(spark.Event) == true)
            {
                continue;
            }

            HitSparkLook look = looks.Of(in spark);
            _sparks[used].Visible = true;
            _sparks[used++].ShowFrame(spark.ShownAge, in look);
        }

        for (int i = used; i < _sparks.Length; i++)
        {
            _sparks[i].Visible = false;
        }
    }
}
