using System;
using System.Globalization;

namespace FightingGame.Input;

/// <summary>
/// A test assignment from the command line (developer option; an assignment chosen on side select wins, ADR-0043):
/// <c>--p1-devices pad2 --p2-devices keyboard</c>. Devices are <c>keyboard</c> and <c>pad1</c>, <c>pad2</c>, … for
/// the joypads connected when the match starts, in roster order.
/// </summary>
public static class DeviceAssignmentArgs
{
    public const string Usage = "--p1-devices <list> --p2-devices <list>: keyboard, pad1, pad2, …";

    private const string PadPrefix = "pad";

    /// <summary>The assignment the options give, or null when there are none. Players without an option read nothing.</summary>
    /// <exception cref="FormatException">An unknown device, a pad that isn't connected, or one device on two players.</exception>
    public static PlayerDevices? Parse(string[] args, int playerCount, DeviceRoster roster)
    {
        var assignment = new PlayerDevices(playerCount);
        bool given = false;
        for (int player = 0; player < playerCount; player++)
        {
            string name = $"--p{player + 1}-devices";
            int index = Array.IndexOf(args, name);
            if (index < 0)
            {
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new FormatException($"{name} needs a list of devices. {Usage}");
            }

            given = true;
            foreach (string device in args[index + 1].Split(',', StringSplitOptions.TrimEntries))
            {
                InputDeviceId id = Device(device, roster);
                if (assignment.PlayerOf(id) >= 0)
                {
                    throw new FormatException($"{device} is on two players. {Usage}");
                }

                assignment.Assign(player, id);
            }
        }

        return given ? assignment : null;
    }

    private static InputDeviceId Device(string name, DeviceRoster roster)
    {
        if (name == "keyboard")
        {
            return InputDeviceId.Keyboard;
        }

        if (name.StartsWith(PadPrefix, StringComparison.Ordinal) &&
            int.TryParse(name.AsSpan(PadPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int pad) &&
            pad >= 1)
        {
            return NthJoypad(roster, pad) ??
                throw new FormatException($"{name} isn't connected ({JoypadCount(roster)} joypads).");
        }

        throw new FormatException($"Unknown device '{name}'. {Usage}");
    }

    private static InputDeviceId? NthJoypad(DeviceRoster roster, int pad)
    {
        int seen = 0;
        foreach (InputDeviceInfo device in roster.Devices)
        {
            if (device.Id.IsJoypad && ++seen == pad)
            {
                return device.Id;
            }
        }

        return null;
    }

    private static int JoypadCount(DeviceRoster roster)
    {
        int count = 0;
        foreach (InputDeviceInfo device in roster.Devices)
        {
            count += device.Id.IsJoypad ? 1 : 0;
        }

        return count;
    }
}
