using Godot;
using Sim.Characters;
using Sim.State;

namespace FightingGame.Presentation.World.Projectiles;

/// <summary>
/// Shows a projectile move's charge effect during its startup, from its first frame to the one before the
/// projectile spawns, at the point it will spawn from. Getting hit out of the move ends it with the move.
/// </summary>
/// <remarks>One node per projectile with a charge effect, made at match start (a fighter is in one move at a time).</remarks>
public partial class ChargeViews : Node3D
{
    // Per owner, per projectile index: the charge node, or null without a charge effect.
    private Node3D?[][] _nodes = [];
    private IChargeArt?[][] _art = [];
    private CharacterData[] _characters = [];

    /// <param name="arts">Per owner and projectile index, from <see cref="ProjectileArtLookup"/>.</param>
    public void Build(CharacterData[] characters, ProjectileArtSettings?[][] arts)
    {
        _characters = characters;
        _nodes = new Node3D?[characters.Length][];
        _art = new IChargeArt?[characters.Length][];
        for (int owner = 0; owner < characters.Length; owner++)
        {
            _nodes[owner] = new Node3D?[arts[owner].Length];
            _art[owner] = new IChargeArt?[arts[owner].Length];
            for (int projectile = 0; projectile < arts[owner].Length; projectile++)
            {
                if (arts[owner][projectile]?.Charge is { } charge &&
                    ProjectileEffects.CreateCharge(charge, out _art[owner][projectile]) is { } node)
                {
                    node.Visible = false;
                    AddChild(node);
                    _nodes[owner][projectile] = node;
                }
            }
        }
    }

    public void ShowFrame(in FighterStates fighters)
    {
        for (int owner = 0; owner < _nodes.Length; owner++)
        {
            ref readonly FighterState fighter = ref fighters[owner];
            CharacterData character = _characters[owner];
            MoveData move = character.Moves[fighter.Move];
            int charging = move.ProjectileCount > 0 ? move.ProjectileStart : -1;
            for (int projectile = 0; projectile < _nodes[owner].Length; projectile++)
            {
                ref readonly ProjectileData data = ref character.Projectiles[projectile];
                bool shown = projectile == charging && fighter.MoveFrame < data.SpawnFrame;
                if (_nodes[owner][projectile] is { } node)
                {
                    node.Visible = shown;
                }

                if (shown && _art[owner][projectile] is { } art)
                {
                    var spawn = ProjectileCentre.SpawnPosition(in data, fighter.Position, fighter.Facing);
                    Vector3 centre = ProjectileCentre.Of(character, in data, spawn, fighter.Facing, 1);
                    art.ShowFrame(fighter.MoveFrame, data.SpawnFrame, centre, fighter.Facing);
                }
            }
        }
    }
}
