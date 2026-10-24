using System.Collections.Generic;
using FightingGame.Input;
using FightingGame.UI.Framework;

namespace FightingGame.UI.SideSelect;

/// <summary>
/// The rules of side select: every connected device starts in the middle and moves one column per left or right; a
/// side holds one device, confirm readies it there, back undoes one step and leaves from the middle. Confirm in the
/// middle takes the first free side and readies at once, so one press is enough for the common case. Devices that
/// connect appear in the middle (or back on the side they left, while it is free); unplugging one frees its side.
/// Moves on once every side the mode needs (<see cref="SideRules"/>) is ready for a moment.
/// </summary>
/// <remarks>Plain C#: the screen hands it presses per device and draws it.</remarks>
public sealed class DeviceSides
{
    private readonly List<SideSelectDevice> _devices = [];

    // Where a device that was unplugged on this screen stood, so it goes back there if it is plugged in again.
    private readonly Dictionary<InputDeviceId, SideColumn> _unplugged = [];
    private readonly SideSelectSettings _settings;
    private long? _readySince;

    public DeviceSides(SideSelectSettings settings)
    {
        _settings = settings;
    }

    public SideRules Rules { get; private set; } = SideRules.Versus;

    /// <summary>Every device on the screen: the roster's order, then the ones that joined later.</summary>
    public IReadOnlyList<SideSelectDevice> Devices => _devices;

    /// <summary>Every side a device plays is taken and ready.</summary>
    public bool IsAllReady
    {
        get
        {
            for (int side = 0; side < Rules.PlayerCount; side++)
            {
                if (On(side) is not { IsReady: true })
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Starts over with <paramref name="connected"/>, each device on the side it had in <paramref name="remembered"/>
    /// (the last assignment chosen this session) or in the middle. A lone device starts on player 1.
    /// </summary>
    public void Begin(IReadOnlyList<InputDeviceInfo> connected, SideRules rules, PlayerDevices? remembered)
    {
        Rules = rules;
        _devices.Clear();
        _unplugged.Clear();
        _readySince = null;
        foreach (InputDeviceInfo device in connected)
        {
            _devices.Add(new SideSelectDevice(device));
        }

        if (remembered is not null)
        {
            PlaceRemembered(remembered);
        }

        // The quick path: one device has nobody to share with, so it doesn't have to slide first.
        if (_devices.Count == 1 && On(0) is null)
        {
            _devices[0].Column = SideSelectColumns.Of(0);
        }
    }

    /// <summary>Keeps the screen in step with the roster (plugging and unplugging); call once per displayed frame.</summary>
    public void Follow(IReadOnlyList<InputDeviceInfo> connected)
    {
        for (int index = _devices.Count - 1; index >= 0; index--)
        {
            SideSelectDevice device = _devices[index];
            if (!Contains(connected, device.Id))
            {
                _unplugged[device.Id] = device.Column;
                _devices.RemoveAt(index);
            }
        }

        foreach (InputDeviceInfo device in connected)
        {
            if (Find(device.Id) is null)
            {
                Join(device);
            }
        }
    }

    /// <summary>True once everyone has been ready for the short "all ready" moment; call once per displayed frame.</summary>
    public bool Update(long nowMilliseconds)
    {
        if (!IsAllReady)
        {
            _readySince = null;
            return false;
        }

        _readySince ??= nowMilliseconds;
        return nowMilliseconds - _readySince >= _settings.AllReadyMilliseconds;
    }

    /// <summary>The device on <paramref name="side"/>, or null while it is free.</summary>
    public SideSelectDevice? On(int side)
    {
        SideColumn column = SideSelectColumns.Of(side);
        foreach (SideSelectDevice device in _devices)
        {
            if (device.Column == column)
            {
                return device;
            }
        }

        return null;
    }

    /// <summary>
    /// 1, 2… among the devices on the screen with the same name as <paramref name="device"/>, in screen order (the
    /// roster's, then joining order); 0 when its name is unique. Two pads of one model would look the same otherwise.
    /// </summary>
    public int NumberOf(SideSelectDevice device)
    {
        int number = 0;
        int sameName = 0;
        foreach (SideSelectDevice other in _devices)
        {
            if (other.Name != device.Name)
            {
                continue;
            }

            sameName++;
            if (ReferenceEquals(other, device))
            {
                number = sameName;
            }
        }

        return sameName > 1 ? number : 0;
    }

    /// <summary>A press on <paramref name="device"/>; true when it leaves the screen (back from the middle).</summary>
    /// <remarks>A device not on the screen yet only appears: its first press doesn't move it.</remarks>
    public bool Press(InputDeviceInfo device, MenuAction actions)
    {
        if (Find(device.Id) is not { } onScreen)
        {
            Join(device);
            return false;
        }

        if ((actions & MenuAction.Back) != 0)
        {
            return Back(onScreen);
        }

        if ((actions & MenuAction.Confirm) != 0)
        {
            Confirm(onScreen);
        }
        else if ((actions & MenuAction.Left) != 0)
        {
            Move(onScreen, -1);
        }
        else if ((actions & MenuAction.Right) != 0)
        {
            Move(onScreen, 1);
        }

        return false;
    }

    /// <summary>The devices on the sides as the match's assignment; it never claims new joypads (ADR-0038 §5).</summary>
    public PlayerDevices ToAssignment()
    {
        var assignment = new PlayerDevices(Rules.PlayerCount);
        for (int side = 0; side < Rules.PlayerCount; side++)
        {
            if (On(side) is { } device)
            {
                assignment.Assign(side, device.Id);
            }
        }

        return assignment;
    }

    private void PlaceRemembered(PlayerDevices remembered)
    {
        for (int side = 0; side < Rules.PlayerCount && side < remembered.PlayerCount; side++)
        {
            foreach (InputDeviceId id in remembered.Of(side))
            {
                if (Find(id) is { Column: SideColumn.NotPlaying } device)
                {
                    device.Column = SideSelectColumns.Of(side);
                    break;
                }
            }
        }
    }

    private void Join(InputDeviceInfo device)
    {
        var joined = new SideSelectDevice(device);
        if (_unplugged.Remove(device.Id, out SideColumn column) && column != SideColumn.NotPlaying &&
            CanTake(SideSelectColumns.SideOf(column)))
        {
            joined.Column = column;
        }

        _devices.Add(joined);
    }

    private static bool Back(SideSelectDevice device)
    {
        if (device.IsReady)
        {
            device.IsReady = false;
            return false;
        }

        if (device.Column != SideColumn.NotPlaying)
        {
            device.Column = SideColumn.NotPlaying;
            return false;
        }

        return true;
    }

    private void Confirm(SideSelectDevice device)
    {
        if (device.Column != SideColumn.NotPlaying)
        {
            device.IsReady = true;
            return;
        }

        for (int side = 0; side < Rules.PlayerCount; side++)
        {
            if (CanTake(side))
            {
                device.Column = SideSelectColumns.Of(side);
                device.IsReady = true;
                return;
            }
        }

        device.Refusals++;
    }

    private void Move(SideSelectDevice device, int step)
    {
        int target = (int)device.Column + step;
        bool isRefused = device.IsReady || target < (int)SideColumn.Player1 || target > (int)SideColumn.Player2 ||
            ((SideColumn)target != SideColumn.NotPlaying && !CanTake(SideSelectColumns.SideOf((SideColumn)target)));
        if (isRefused)
        {
            device.Refusals++;
            return;
        }

        device.Column = (SideColumn)target;
    }

    private bool CanTake(int side) => Rules.RoleOf(side) == SideRole.Player && On(side) is null;

    private SideSelectDevice? Find(InputDeviceId id)
    {
        foreach (SideSelectDevice device in _devices)
        {
            if (device.Id == id)
            {
                return device;
            }
        }

        return null;
    }

    private static bool Contains(IReadOnlyList<InputDeviceInfo> devices, InputDeviceId id)
    {
        foreach (InputDeviceInfo device in devices)
        {
            if (device.Id == id)
            {
                return true;
            }
        }

        return false;
    }
}
