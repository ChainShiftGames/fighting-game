using Netcode.Drivers.Training;

namespace FightingGame.Training;

/// <summary>Training mode's player settings (ARCHITECTURE.md §6): chosen in the training menu, kept between sessions.</summary>
/// <remarks>Later training options (frame meter shown, P3-31) are added here as further properties.</remarks>
public sealed record TrainingSettings
{
    public DummyMode Dummy { get; init; } = DummyMode.Stand;
}
