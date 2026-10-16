using Sim.State;

namespace Sim.Input;

/// <summary>The first step of every logic frame: cleans each player's raw input word and records it.</summary>
/// <remarks>
/// Cleaning drops bits the button set doesn't define and applies the fixed SOCD rule (ADR-0002). Both happen here,
/// in the logic, so local play, online, replays and bots all see exactly the same words.
/// </remarks>
public sealed class InputStep
{
    private readonly ushort _definedBits;

    public InputStep(ButtonSet buttons)
    {
        _definedBits = (ushort)(InputWord.DirectionMask | buttons.AllButtons);
    }

    public void Run(ref InputHistories histories, InputWord player1, InputWord player2)
    {
        histories[0].Record(Clean(player1));
        histories[1].Record(Clean(player2));
    }

    /// <summary>The word the logic sees for a raw device word.</summary>
    public InputWord Clean(InputWord raw) => SocdResolver.Resolve(new InputWord((ushort)(raw.Bits & _definedBits)));
}
