namespace FightingGame.Input.Profiles;

/// <summary>The layouts of the devices connected now, found by Godot's joypad index through the roster.</summary>
public sealed class ConnectedProfiles : IMenuProfiles
{
    private readonly ControlProfiles _profiles;
    private readonly DeviceRoster _roster;

    public ConnectedProfiles(ControlProfiles profiles, DeviceRoster roster)
    {
        _profiles = profiles;
        _roster = roster;
    }

    public BindingProfile Keyboard => _profiles.For(InputDeviceId.Keyboard);

    public BindingProfile? Joypad(int joypad) =>
        _roster.TryFindJoypad(joypad, out InputDeviceInfo device) ? _profiles.For(device.Id) : null;
}
