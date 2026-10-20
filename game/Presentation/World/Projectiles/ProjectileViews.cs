using Godot;
using Sim.Characters;
using Sim.State;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// Shows every live projectile with the flight effect its owner's art file names for the move that spawned it. One
/// without an effect shows only in the hitbox overlay. The first shown effect that gives light lights the fighters.
/// </summary>
/// <remarks>
/// Effect nodes are made at match start, one per entity slot a projectile could take, so a frame never allocates.
/// </remarks>
public partial class ProjectileViews : Node3D
{
    // Per owner, per projectile index, per slot of the owner's block: the effect node, or null without an effect.
    private Node3D?[][][] _nodes = [];
    private IProjectileArt?[][][] _art = [];
    private CharacterData[] _characters = [];
    private readonly EffectLighting _lighting = new();

    /// <summary>Makes the effect nodes for P1's and P2's projectiles; call once, before the view joins the scene.</summary>
    /// <param name="arts">Per owner and projectile index, from <see cref="ProjectileArtLookup"/>.</param>
    public void Build(CharacterData[] characters, ProjectileArtSettings?[][] arts)
    {
        _characters = characters;
        _nodes = new Node3D?[characters.Length][][];
        _art = new IProjectileArt?[characters.Length][][];
        for (int owner = 0; owner < characters.Length; owner++)
        {
            _nodes[owner] = new Node3D?[arts[owner].Length][];
            _art[owner] = new IProjectileArt?[arts[owner].Length][];
            for (int projectile = 0; projectile < arts[owner].Length; projectile++)
            {
                _nodes[owner][projectile] = new Node3D?[EntityPool.MaxPerPlayer];
                _art[owner][projectile] = new IProjectileArt?[EntityPool.MaxPerPlayer];
                if (arts[owner][projectile] is not { } art)
                {
                    continue;
                }

                for (int slot = 0; slot < EntityPool.MaxPerPlayer; slot++)
                {
                    _nodes[owner][projectile][slot] =
                        ProjectileEffects.CreateFlight(art.Flight, out _art[owner][projectile][slot]);
                    if (_nodes[owner][projectile][slot] is { } node)
                    {
                        node.Visible = false;
                        AddChild(node);
                    }
                }
            }
        }
    }

    /// <summary>Shows the projectiles in <paramref name="entities"/> and hides every other effect node.</summary>
    public void ShowFrame(in EntityPool entities)
    {
        bool lit = false;
        for (int owner = 0; owner < _nodes.Length; owner++)
        {
            int first = EntityPool.FirstSlotOf(owner);
            for (int slot = 0; slot < EntityPool.MaxPerPlayer; slot++)
            {
                ref readonly EntityState entity = ref entities.Slots[first + slot];
                for (int projectile = 0; projectile < _nodes[owner].Length; projectile++)
                {
                    bool shown = entity.IsActive == 1 && entity.Owner == owner && entity.Projectile == projectile;
                    if (_nodes[owner][projectile][slot] is { } node)
                    {
                        node.Visible = shown;
                    }

                    if (shown && _art[owner][projectile][slot] is { } art)
                    {
                        CharacterData character = _characters[owner];
                        Vector3 centre = ProjectileCentre.Of(
                            character, in character.Projectiles[projectile], entity.Position, entity.Facing, entity.Frame);
                        art.ShowFrame(entity.Frame, centre, entity.Facing);
                        if (!lit && art is IEffectLightSource source && source.LightNow(out Vector3 at, out Color colour, out float reach))
                        {
                            _lighting.Show(at, colour, reach);
                            lit = true;
                        }
                    }
                }
            }
        }

        if (!lit)
        {
            _lighting.Clear();
        }
    }
}
