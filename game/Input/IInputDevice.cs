using Sim.Input;

namespace FightingGame.Input;

/// <summary>One physical device (keyboard bindings, gamepad) that reports which directions and buttons are held now.</summary>
public interface IInputDevice
{
    /// <summary>The raw word: bound bits only, opposite directions kept (the logic cleans SOCD).</summary>
    InputWord Read();
}
