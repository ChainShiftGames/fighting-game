using Godot;

namespace FightingGame.Input;

/// <summary>What the keyboard and joypads hold right now: the seam between Godot's input and the bindings.</summary>
public interface IDeviceState
{
    /// <summary>By physical key, so bindings stay in the same place on AZERTY and other keyboard layouts.</summary>
    bool IsKeyHeld(Key key);

    bool IsJoyButtonHeld(int device, JoyButton button);

    /// <summary>Sticks −1 to 1 (Y points down), triggers 0 to 1.</summary>
    float JoyAxisValue(int device, JoyAxis axis);
}
