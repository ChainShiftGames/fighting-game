using Backdash;
using Sim.Input;

namespace Netcode.Online;

/// <summary>Lowers the input delay of a running Backdash session's local players.</summary>
internal sealed class SessionInputDelay : IInputDelayControl
{
    private readonly INetcodeSession<InputWord> _session;

    public SessionInputDelay(INetcodeSession<InputWord> session)
    {
        _session = session;
    }

    // Backdash drops the local inputs whose delayed frames were already sent, so the other PC still gets one input
    // per frame. Raising instead would skip frames on the wire, which Backdash 0.7.8 rejects (P2-06).
    public void LowerLocalInputDelay(int frames) => _session.SetFrameDelay(frames);
}
