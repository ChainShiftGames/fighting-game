using Godot;

namespace FightingGame.Presentation.World;

/// <summary>The 3D camera, facing the gameplay plane square-on and placed from the frame's <see cref="CameraFraming"/>.</summary>
public partial class MatchCamera : Camera3D
{
    // Developer settings (presentation, ADR-0027 §4): orthographic until the art direction asks for stage depth; the
    // perspective angle is narrow, as fighting games use, and the camera keeps OrthographicDistance from the plane.
    private const bool UsePerspective = false;
    private const float PerspectiveFieldOfView = 25f;
    private const float OrthographicDistance = 20f;
    private const float ViewDepth = 200f;

    public override void _Ready()
    {
        Projection = UsePerspective ? ProjectionType.Perspective : ProjectionType.Orthogonal;
        Fov = PerspectiveFieldOfView;
        KeepAspect = KeepAspectEnum.Height;
        Far = ViewDepth;
        Current = true;
    }

    public void Frame(CameraFraming framing)
    {
        float distance = OrthographicDistance;
        if (Projection == ProjectionType.Perspective)
        {
            distance = framing.PerspectiveDistance(Fov);
        }
        else
        {
            Size = framing.VisibleHeight;
        }

        Position = framing.Centre + new Vector3(0f, 0f, distance);
    }
}
