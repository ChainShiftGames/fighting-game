using Godot;

namespace FightingGame.Presentation.World;

/// <summary>The 3D camera, facing the gameplay plane square-on and placed from the frame's <see cref="CameraFraming"/>.</summary>
public partial class MatchCamera : Camera3D
{
    // Developer settings (presentation, ADR-0027 §4): the perspective angle is narrow, as fighting games use, and the
    // orthographic camera keeps OrthographicDistance from the plane.
    private const float PerspectiveFieldOfView = 25f;
    private const float OrthographicDistance = 20f;
    private const float ViewDepth = 200f;

    /// <summary>Perspective for stages with depth, else orthographic (stage art file); set before the camera is ready.</summary>
    public bool UsePerspective { get; set; }

    public override void _Ready()
    {
        Projection = UsePerspective ? ProjectionType.Perspective : ProjectionType.Orthogonal;
        Fov = PerspectiveFieldOfView;
        KeepAspect = KeepAspectEnum.Height;
        Far = ViewDepth;
        Current = true;
    }

    /// <summary>The camera's distance to the gameplay plane, from the last <see cref="Frame"/>.</summary>
    public float PlaneDistance { get; private set; }

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

        PlaneDistance = distance;
        Position = framing.Centre + new Vector3(0f, 0f, distance);
    }
}
