using Sim.Input;

namespace FightingGame.Input;

/// <summary>One physical device (keyboard half, gamepad) that reports which directions and buttons are held now.</summary>
public interface IInputDevice
{
    InputWord Read();
}
