#if TOOLS
using System.Collections.Generic;
using System.Text.Json.Nodes;
using FightingGame.Addons.CharacterEditor.Documents;
using FightingGame.Addons.CharacterEditor.Map;
using FightingGame.Presentation.World;
using FightingGame.Presentation.World.Projectiles;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// The move's projectile effects in the <see cref="ModelPreview"/>, as the match draws them (ADR-0028 §9): the charge
/// at the spawn point during the startup, then the projectile flying, both from the art file's entry for the move.
/// </summary>
[Tool]
public partial class PreviewProjectiles : Node3D
{
    private readonly List<Node3D> _nodes = [];
    private IReadOnlyDictionary<string, ProjectileArtSettings> _arts = new Dictionary<string, ProjectileArtSettings>();
    private string? _moveId;
    private int _count;
    private Node3D? _charge;
    private IChargeArt? _chargeArt;
    private readonly List<(Node3D Node, IProjectileArt? Art)> _flights = [];

    /// <summary>The character's projectile effects, by the move that spawns them; empty for none.</summary>
    public void ShowCharacter(IReadOnlyDictionary<string, ProjectileArtSettings> arts)
    {
        _arts = arts;
        Clear();
    }

    public void ShowFrame(JsonObject move, int frame)
    {
        string? id = JsonNodeValue.Text(move[MoveFields.Id]);
        int count = ProjectileFields.All(move).Count;
        if (id != _moveId || count != _count)
        {
            Build(id, count);
        }

        if (_charge is not null)
        {
            _charge.Visible = false;
        }

        foreach ((Node3D node, _) in _flights)
        {
            node.Visible = false;
        }

        foreach (ProjectileOnFrame projectile in ProjectilePreview.Of(move, frame))
        {
            Vector3 centre = Centre(projectile);
            if (!projectile.HasSpawned)
            {
                // The charge belongs to the move's first projectile, as in the match.
                if (projectile.Index == 0 && _charge is not null && _chargeArt is not null)
                {
                    _charge.Visible = true;
                    _chargeArt.ShowFrame(frame, projectile.SpawnFrame, centre, 1);
                }
            }
            else if (projectile.Index < _flights.Count && _flights[projectile.Index] is (Node3D node, IProjectileArt art))
            {
                node.Visible = true;
                int lifetime = ProjectileFields.LifetimeOf(projectile.Projectile) ?? projectile.LifeFrame;
                art.ShowFrame(projectile.LifeFrame, centre, 1, lifetime);
            }
        }
    }

    private void Build(string? moveId, int count)
    {
        Clear();
        _moveId = moveId;
        _count = count;
        if (moveId is null || !_arts.TryGetValue(moveId, out ProjectileArtSettings? art))
        {
            return;
        }

        if (art.Charge is { } charge)
        {
            _charge = Add(ProjectileEffects.CreateCharge(charge, out _chargeArt));
        }

        for (int i = 0; i < count; i++)
        {
            Node3D? node = Add(ProjectileEffects.CreateFlight(art.Flight, out IProjectileArt? flight));
            if (node is not null)
            {
                _flights.Add((node, flight));
            }
        }
    }

    private Node3D? Add(Node3D? node)
    {
        if (node is not null)
        {
            node.Visible = false;
            AddChild(node);
            _nodes.Add(node);
        }

        return node;
    }

    private void Clear()
    {
        foreach (Node3D node in _nodes)
        {
            node.QueueFree();
        }

        _nodes.Clear();
        _flights.Clear();
        _charge = null;
        _chargeArt = null;
        _moveId = null;
        _count = 0;
    }

    // Effects are drawn at the centre of the projectile's hitbox, as ProjectileCentre does in the match.
    private static Vector3 Centre(ProjectileOnFrame projectile)
    {
        if (projectile.Boxes.Count == 0)
        {
            return new Vector3(WorldScale.ToMetres(projectile.X), WorldScale.ToMetres(projectile.Y), 0f);
        }

        BoxArea box = projectile.Boxes[0];
        return new Vector3(WorldScale.ToMetres(box.X + (box.Width / 2f)), WorldScale.ToMetres(box.Y + (box.Height / 2f)), 0f);
    }
}
#endif
