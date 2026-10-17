using System;
using System.Linq;
using Netcode.Drivers;
using Netcode.Replays;
using Sim.State;

namespace FightingGame.Online;

/// <summary>
/// The replay of one running match, handed over once: when the match is over, or whatever is final when it ends early.
/// </summary>
/// <remarks>
/// Online, the newest frames may still run on predicted inputs that a rollback replaces, so only the frames the
/// driver reports final are handed over: they ran with both players' confirmed inputs.
/// </remarks>
public sealed class MatchRecording
{
    private const int NotOver = -1;

    private readonly ReplayRecorder _recorder;
    private readonly IMatchDriver<MatchState> _driver;
    private readonly IConfirmedFrameReadout? _confirmed;
    private int _overAtFrameCount = NotOver;
    private bool _handedOver;

    /// <param name="recorder">Records the driver's match (<see cref="MatchLineup.RecordedSimulation"/>).</param>
    /// <param name="confirmed">Which frames are final (online); null when every frame is final once it runs.</param>
    /// <param name="kind">What kind of match it is, e.g. "local" or "online-p2", for the file name.</param>
    public MatchRecording(
        ReplayRecorder recorder, IMatchDriver<MatchState> driver, IConfirmedFrameReadout? confirmed, string kind)
    {
        _recorder = recorder;
        _driver = driver;
        _confirmed = confirmed;
        Kind = kind;
    }

    public string Kind { get; }

    /// <summary>Recorded frames no rollback can change any more.</summary>
    public int ConfirmedFrameCount =>
        Math.Clamp(_confirmed?.ConfirmedFrame ?? _driver.CurrentFrame, 0, _recorder.FrameCount);

    /// <summary>
    /// Call once per displayed frame. The whole match up to its result, once it is over and that part is confirmed;
    /// null before that and after the replay was handed over.
    /// </summary>
    public Replay? TakeFinishedReplay()
    {
        // Online the result may first show on predicted inputs; a rollback that takes it back starts the wait again.
        if (_driver.State.Round.Phase != RoundPhase.MatchOver)
        {
            _overAtFrameCount = NotOver;
            return null;
        }

        if (_overAtFrameCount == NotOver)
        {
            _overAtFrameCount = _driver.CurrentFrame;
        }

        return ConfirmedFrameCount >= _overAtFrameCount ? Take(_overAtFrameCount) : null;
    }

    /// <summary>When the match is left: every confirmed frame, unless the finished replay was already handed over.</summary>
    public Replay? TakeOnEnd() => Take(ConfirmedFrameCount);

    private Replay? Take(int frameCount)
    {
        if (_handedOver || frameCount == 0)
        {
            return null;
        }

        _handedOver = true;
        Replay recorded = _recorder.ToReplay();
        return new Replay(recorded.Header, recorded.Inputs.Take(frameCount).ToArray());
    }
}
