namespace Netcode.Online;

/// <summary>
/// Keeps this PC's input delay at the player's choice or, by default, follows the averaged ping, changing it only
/// between rounds (ADR-0005 §4).
/// </summary>
/// <remarks>
/// <para>
/// Lowering the delay makes Backdash drop a few local inputs. Between rounds nobody controls their fighter
/// (Sim.Match.RoundInputLock), so those inputs don't matter and the delay never shifts while players fight.
/// </para>
/// <para>
/// A running match can only lower it (<see cref="IInputDelayControl"/>): each match starts at the highest automatic
/// delay and drops when the ping allows; a ping that rises again keeps the lowered delay until the next match.
/// </para>
/// </remarks>
public sealed class InputDelayController
{
    private readonly InputDelaySettings _settings;
    private readonly IConnectionReadout _connection;
    private readonly IInputDelayControl _control;
    private readonly PingWindow _pings;
    private readonly int _framesToConfirmRoundOver;
    private int _framesSinceReading;
    private int _framesBetweenRounds;

    /// <param name="connection">Backdash's ping readings.</param>
    /// <param name="control">The running session's local input delay, already at <see cref="InputDelaySettings.StartingFrames"/>.</param>
    /// <param name="rollbackFrames">The rollback window (developer setting): how long a round end can still be undone.</param>
    public InputDelayController(
        InputDelaySettings settings, IConnectionReadout connection, IInputDelayControl control, int rollbackFrames)
    {
        settings.Automatic.Validate();
        _settings = settings;
        _connection = connection;
        _control = control;
        _pings = new PingWindow(settings.Automatic.PingSamples);

        // Once the round-over pause is older than the rollback window, the round end is confirmed: a predicted KO
        // that a rollback undoes never changes the delay mid-round.
        _framesToConfirmRoundOver = rollbackFrames + 1;
        CurrentFrames = settings.StartingFrames;
    }

    /// <summary>The delay this PC's inputs have now.</summary>
    public int CurrentFrames { get; private set; }

    /// <summary>Call after every logic frame the match ran.</summary>
    /// <param name="betweenRounds">The newest state is in a round-over pause, with another round to follow.</param>
    public void AfterFrame(bool betweenRounds)
    {
        if (_settings.PlayerOverrideFrames is not null)
        {
            return;
        }

        ReadPingWhenDue();
        if (!betweenRounds)
        {
            _framesBetweenRounds = 0;
            return;
        }

        _framesBetweenRounds++;
        if (_framesBetweenRounds == _framesToConfirmRoundOver)
        {
            Recalculate();
        }
    }

    private void ReadPingWhenDue()
    {
        _framesSinceReading++;
        if (_framesSinceReading < _settings.Automatic.PingSampleIntervalFrames)
        {
            return;
        }

        _framesSinceReading = 0;

        // Backdash reports 0 until its first round trip; that is no reading, not a perfect connection.
        if (_connection.TryReadConnection(out ConnectionStats stats) && stats.PingMilliseconds > 0)
        {
            _pings.Add(stats.PingMilliseconds);
        }
    }

    private void Recalculate()
    {
        if (!_pings.TryGetMedian(out int ping))
        {
            return;
        }

        // A higher delay waits for the next match, which starts at the highest one (IInputDelayControl).
        int frames = _settings.Automatic.FramesFor(ping);
        if (frames < CurrentFrames)
        {
            _control.LowerLocalInputDelay(frames);
            CurrentFrames = frames;
        }
    }
}
