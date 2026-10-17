namespace Sim.Modules.Install;

/// <summary>An install's slice of the match state, in its owner's mechanic slot.</summary>
/// <remarks>Scenarios read the time left as <c>p1 mechanic 0</c>.</remarks>
public struct InstallState : IModuleState
{
    /// <summary>Frames until the install ends; 0 while it isn't running.</summary>
    public int FramesLeft;
}
