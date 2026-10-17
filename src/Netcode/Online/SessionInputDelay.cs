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
    // per frame. Raising instead skips a frame on the wire: Backdash 0.7.8 sometimes trips an assertion, otherwise
    // the other PC silently reads the next inputs a frame early, a desync (LocalInputDelayTests, P2-06).
    public void LowerLocalInputDelay(int frames) => _session.SetFrameDelay(frames);
}
