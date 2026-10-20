using System.Collections.Generic;
using Godot;
using Sim.Math;

namespace FightingGame.Presentation.World;

/// <summary>
/// Placeholder fighter in the 3D scene: a block the size of its body box from the data, with a nose on the side it
/// faces, so a crouch is shorter and a move that shifts the body shows it.
/// </summary>
/// <remarks>The skeletal model replaces this through <see cref="IFighterArt"/> (ADR-0027 §6).</remarks>
public partial class PlaceholderFighterModel : Node3D, IFighterArt
{
    // Developer settings (presentation): how deep the block is, and the nose's size and height as shares of the body.
    private const float BodyDepth = 0.4f;
    private const float NoseShareOfWidth = 0.3f;
    private const float NoseShareFromTop = 0.25f;

    private readonly MeshInstance3D _body = new() { Mesh = new BoxMesh() };
    private readonly MeshInstance3D _nose = new() { Mesh = new BoxMesh() };

    // Godot needs a parameterless constructor for nodes; the colour comes in once, before the first frame.
    public void Paint(Color color)
    {
        _body.MaterialOverride = new StandardMaterial3D { AlbedoColor = color };
        _nose.MaterialOverride = new StandardMaterial3D { AlbedoColor = color.Lightened(0.6f) };
    }

    public override void _Ready()
    {
        AddChild(_body);
        AddChild(_nose);
    }

    // A block body: the readouts draw the attack.
    public bool HasLimbs => false;

    public void ShowPose(FighterPose pose, IntVector2 position, int facing, List<WorldBox> boxes)
    {
        AxisAlignedBox body = FighterBoxes.Body(boxes);
        Visible = !body.IsEmpty;
        if (!Visible)
        {
            return;
        }

        float width = WorldScale.ToMetres(body.Width);
        float height = WorldScale.ToMetres(body.Height);
        _body.Position = WorldScale.CentreOf(body);
        _body.Scale = new Vector3(width, height, BodyDepth);

        float nose = width * NoseShareOfWidth;
        float edge = WorldScale.ToMetres(facing > 0 ? body.MaxX : body.MinX);
        float noseY = WorldScale.ToMetres(body.MaxY) - (height * NoseShareFromTop);
        _nose.Position = new Vector3(edge + (facing * nose / 2f), noseY, 0f);
        _nose.Scale = new Vector3(nose, nose, nose);
    }
}
