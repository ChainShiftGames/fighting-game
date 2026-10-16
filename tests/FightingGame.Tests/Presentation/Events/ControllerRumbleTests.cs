using System.Collections.Generic;
using FightingGame.Presentation.Events;
using Sim.Events;
using Sim.Math;
using Xunit;

namespace FightingGame.Tests.Presentation.Events;

public sealed class ControllerRumbleTests
{
    private readonly RecordingMotor _motor = new();
    private readonly ControllerRumble _rumble;

    public ControllerRumbleTests()
    {
        _rumble = new ControllerRumble(_motor);
    }

    [Fact]
    public void AHitRumblesBothPlayersHarderForTheOneHit()
    {
        _rumble.Play(Event(10, FrameEventKind.Hit, attacker: 0));

        Assert.Equal([1, 0], _motor.Started.ConvertAll(pulse => pulse.Player));
        Assert.True(_motor.Started[0].Strength > _motor.Started[1].Strength);
    }

    [Fact]
    public void CancellingAHitStopsItsRumble()
    {
        FrameEvent hit = Event(10, FrameEventKind.Hit, attacker: 0);
        _rumble.Play(hit);

        _rumble.Cancel(hit);

        Assert.Equal([0, 1], _motor.Stopped);
    }

    [Fact]
    public void CancellingAnOlderEventLeavesANewerPulseRunning()
    {
        FrameEvent block = Event(10, FrameEventKind.Block, attacker: 0);
        FrameEvent hit = Event(12, FrameEventKind.Hit, attacker: 0);
        _rumble.Play(block);
        _rumble.Play(hit);

        _rumble.Cancel(block);

        Assert.Empty(_motor.Stopped);
    }

    private static FrameEvent Event(int frame, FrameEventKind kind, int attacker) =>
        new(frame, kind, attacker, occurrence: 0, IntVector2.Zero, detail: 1);

    private sealed class RecordingMotor : IRumbleMotor
    {
        public List<(int Player, float Strength)> Started { get; } = [];

        public List<int> Stopped { get; } = [];

        public void Start(int player, float weak, float strong, float seconds) => Started.Add((player, strong));

        public void Stop(int player) => Stopped.Add(player);
    }
}
