using FightingGame.Input;

namespace FightingGame.UI.SideSelect;

/// <summary>One device on side select: its icon, which column it is in, whether it is ready.</summary>
public sealed class SideSelectDevice
{
    internal SideSelectDevice(InputDeviceInfo device)
    {
        Id = device.Id;
        Name = device.Name;
        Icon = DeviceIcons.Of(device);
    }

    public InputDeviceId Id { get; }

    /// <summary>The roster's name: the joypad's, or "Keyboard" (the view translates that one).</summary>
    public string Name { get; }

    public DeviceIcon Icon { get; }

    public SideColumn Column { get; internal set; } = SideColumn.NotPlaying;

    /// <summary>0 or 1 while on a side; -1 in the middle.</summary>
    public int Side => SideSelectColumns.SideOf(Column);

    /// <summary>Confirmed on its side; only back moves it again.</summary>
    public bool IsReady { get; internal set; }

    /// <summary>How many moves were refused so far; the view shakes the icon each time it goes up.</summary>
    public int Refusals { get; internal set; }
}
