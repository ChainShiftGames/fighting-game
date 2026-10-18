using Godot;
using Sim.Characters;
using Sim.Math;
using Sim.State;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// Shows every live projectile with the effect its owner's art file names for the move that spawned it
/// (<c>"projectiles": { "236c": "dark_fireball" }</c>). One without an effect shows only in the hitbox overlay.
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

    /// <summary>Makes the effect nodes for P1's and P2's projectiles; call once, before the view joins the scene.</summary>
    public void Build(CharacterData[] characters)
    {
        _characters = characters;
        _nodes = new Node3D?[characters.Length][][];
        _art = new IProjectileArt?[characters.Length][][];
        for (int owner = 0; owner < characters.Length; owner++)
        {
            BuildOwner(owner, characters[owner]);
        }
    }

    /// <summary>Shows the projectiles in <paramref name="entities"/> and hides every other effect node.</summary>
    public void ShowFrame(in EntityPool entities)
    {
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
                        art.ShowFrame(entity.Frame, CentreOf(in entity, _characters[owner]), entity.Facing);
                    }
                }
            }
        }
    }

    private void BuildOwner(int owner, CharacterData character)
    {
        string[] moveIds = ProjectileMoves.Of(character);
        FighterArtSettings? settings = FighterArtChoice.ReadSettings(character.Id);
        _nodes[owner] = new Node3D?[moveIds.Length][];
        _art[owner] = new IProjectileArt?[moveIds.Length][];
        for (int projectile = 0; projectile < moveIds.Length; projectile++)
        {
            _nodes[owner][projectile] = new Node3D?[EntityPool.MaxPerPlayer];
            _art[owner][projectile] = new IProjectileArt?[EntityPool.MaxPerPlayer];
            if (settings is null || !settings.Projectiles.TryGetValue(moveIds[projectile], out string? effect))
            {
                continue;
            }

            for (int slot = 0; slot < EntityPool.MaxPerPlayer; slot++)
            {
                Node3D? node = ProjectileEffects.Create(effect, out IProjectileArt? art);
                if (node is not null)
                {
                    node.Visible = false;
                    AddChild(node);
                }

                _nodes[owner][projectile][slot] = node;
                _art[owner][projectile][slot] = art;
            }
        }
    }

    // The centre of the hitbox it has on this frame (mirrored with its facing), or its origin without one.
    private static Vector3 CentreOf(in EntityState entity, CharacterData character)
    {
        ProjectileData data = character.Projectiles[entity.Projectile];
        foreach (BoxData box in character.BoxesOf(in data))
        {
            if (entity.Frame >= box.FirstFrame && entity.Frame <= box.LastFrame)
            {
                AxisAlignedBox area = entity.Facing < 0 ? box.Area.MirrorX() : box.Area;
                return WorldScale.CentreOf(area.Translate(entity.Position));
            }
        }

        return WorldScale.ToWorld(entity.Position);
    }
}
