using System;
using FightingGame.Presentation.Events;
using Godot;
using Sim.Events;
using Sim.State;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// Shows the live projectile impacts with the impact effect their owner's art file names; an impact without one
/// keeps the plain hit spark (see <see cref="HasArt"/>).
/// </summary>
/// <remarks>A few nodes per projectile with an impact effect, made at match start, so a frame never allocates.</remarks>
public partial class ImpactViews : Node3D
{
    // Developer setting (presentation): impacts of one projectile on screen at once; more are not drawn.
    private const int NodesPerProjectile = 4;

    // Per owner, per projectile index, per node: the impact node, or null without an impact effect.
    private Node3D?[][][] _nodes = [];
    private IImpactArt?[][][] _art = [];
    private int[][] _used = [];

    /// <param name="arts">Per owner and projectile index, from <see cref="ProjectileArtLookup"/>.</param>
    public void Build(ProjectileArtSettings?[][] arts)
    {
        _nodes = new Node3D?[arts.Length][][];
        _art = new IImpactArt?[arts.Length][][];
        _used = new int[arts.Length][];
        for (int owner = 0; owner < arts.Length; owner++)
        {
            _nodes[owner] = new Node3D?[arts[owner].Length][];
            _art[owner] = new IImpactArt?[arts[owner].Length][];
            _used[owner] = new int[arts[owner].Length];
            for (int projectile = 0; projectile < arts[owner].Length; projectile++)
            {
                _nodes[owner][projectile] = new Node3D?[NodesPerProjectile];
                _art[owner][projectile] = new IImpactArt?[NodesPerProjectile];
                if (arts[owner][projectile]?.Impact is not { } impact)
                {
                    continue;
                }

                for (int i = 0; i < NodesPerProjectile; i++)
                {
                    _nodes[owner][projectile][i] = ProjectileEffects.CreateImpact(impact, out _art[owner][projectile][i]);
                    if (_nodes[owner][projectile][i] is { } node)
                    {
                        node.Visible = false;
                        AddChild(node);
                    }
                }
            }
        }
    }

    /// <summary>Whether <paramref name="frameEvent"/> is a projectile impact drawn here instead of as a hit spark.</summary>
    public bool HasArt(FrameEvent frameEvent) =>
        ProjectileImpacts.KindOf(frameEvent.Kind) is not null && Owns(frameEvent) &&
        _nodes[frameEvent.Fighter][frameEvent.Detail][0] is not null;

    /// <param name="impacts">From <see cref="ProjectileImpacts.Live"/>.</param>
    /// <param name="latestFrame">The newest simulated frame, the one on screen (<c>MatchState.Frame</c> − 1).</param>
    public void ShowFrame(ReadOnlySpan<FrameEvent> impacts, int latestFrame, in FighterStates fighters)
    {
        foreach (int[] used in _used)
        {
            Array.Clear(used);
        }

        foreach (FrameEvent impact in impacts)
        {
            if (!HasArt(impact) || _used[impact.Fighter][impact.Detail] == NodesPerProjectile)
            {
                continue;
            }

            int i = _used[impact.Fighter][impact.Detail]++;
            ImpactKind kind = ProjectileImpacts.KindOf(impact.Kind)!.Value;
            int facing = FlightDirection(impact, in fighters);
            _nodes[impact.Fighter][impact.Detail][i]!.Visible = true;
            _art[impact.Fighter][impact.Detail][i]!.ShowFrame(
                latestFrame - impact.Frame, kind, WorldScale.ToWorld(impact.Position), facing);
        }

        for (int owner = 0; owner < _nodes.Length; owner++)
        {
            for (int projectile = 0; projectile < _nodes[owner].Length; projectile++)
            {
                for (int i = _used[owner][projectile]; i < NodesPerProjectile; i++)
                {
                    if (_nodes[owner][projectile][i] is { } node)
                    {
                        node.Visible = false;
                    }
                }
            }
        }
    }

    private bool Owns(FrameEvent frameEvent) =>
        frameEvent.Fighter >= 0 && frameEvent.Fighter < _nodes.Length &&
        frameEvent.Detail >= 0 && frameEvent.Detail < _nodes[frameEvent.Fighter].Length;

    // The event doesn't carry the projectile's direction: it flew towards the owner's opponent.
    private static int FlightDirection(FrameEvent impact, in FighterStates fighters)
    {
        int opponentX = fighters[1 - impact.Fighter].Position.X;
        return opponentX == impact.Position.X ? fighters[impact.Fighter].Facing : Math.Sign(opponentX - impact.Position.X);
    }
}
