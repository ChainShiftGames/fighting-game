namespace Netcode.Online;

/// <summary>Lowers how many frames late this PC's own inputs apply, during a running match.</summary>
/// <remarks>
/// Only lower: a raise would need two local inputs in one frame to fill the gap, and Backdash takes exactly one per
/// frame. A match starts at its highest delay instead (<see cref="InputDelaySettings.StartingFrames"/>).
/// </remarks>
public interface IInputDelayControl
{
    void LowerLocalInputDelay(int frames);
}
