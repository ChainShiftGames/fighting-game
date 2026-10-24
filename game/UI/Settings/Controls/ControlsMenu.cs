using System;
using System.Collections.Generic;
using FightingGame.Input;
using FightingGame.Input.Profiles;
using FightingGame.UI.Framework;
using Sim.Input;

namespace FightingGame.UI.Settings.Controls;

/// <summary>
/// The controls screens opened from the options menu's Controls tab, for one device at a time: they sit over the
/// options menu (<see cref="Path"/>), edit that device's layout at once, and save when the last one closes.
/// "Press a control" (<see cref="Capture"/>) takes the next control the device presses; a control another input of
/// the same layer already uses asks first (<see cref="DuplicateBindingScreen"/>).
/// </summary>
public sealed class ControlsMenu
{
    private const int DirectionCount = 4;

    private readonly ControlProfiles _profiles;
    private readonly string[] _directions;
    private readonly string[] _buttons;
    private readonly ProfileTester _tester;
    private readonly ControlsMenuTiming _timing;
    private readonly Func<long> _now;
    private readonly Action _save;
    private readonly List<IMenuScreen> _open = [];

    /// <param name="gameInputs">The inputs a layout binds: the four directions, then the button set's buttons.</param>
    /// <param name="now">The menu clock in milliseconds, for the capture's time limit.</param>
    /// <param name="save">Writes the layouts to the controls file.</param>
    public ControlsMenu(
        ControlProfiles profiles,
        string[] gameInputs,
        ProfileTester tester,
        ControlsMenuTiming timing,
        Func<long> now,
        Action save)
    {
        _profiles = profiles;
        _directions = gameInputs[..DirectionCount];
        _buttons = gameInputs[DirectionCount..];
        _tester = tester;
        _timing = timing;
        _now = now;
        _save = save;
    }

    /// <summary>The open controls screens, bottom first; empty while none is open.</summary>
    public IReadOnlyList<IMenuScreen> Path => _open;

    public bool IsOpen => _open.Count > 0;

    /// <summary>The device being edited.</summary>
    public InputDeviceInfo Device { get; private set; }

    public BindingProfile Profile => _profiles.For(Device.Id);

    public bool IsOverMatch { get; private set; }

    /// <summary>The "press a control" wait going on, or null.</summary>
    public ControlCapture? Capture { get; private set; }

    public List<ControlPreset> Presets => _profiles.PresetsFor(Device.Id.Kind);

    /// <summary>The game inputs up, down, left and right.</summary>
    public IReadOnlyList<string> Directions => _directions;

    /// <summary>The button set's buttons.</summary>
    public IReadOnlyList<string> Buttons => _buttons;

    /// <param name="isOverMatch">Opened from the pause menu's options, over the match.</param>
    public void Open(InputDeviceInfo device, bool isOverMatch)
    {
        CloseAll();
        Device = device;
        IsOverMatch = isOverMatch;
        Push(new DeviceControlsScreen(this));
    }

    public void Push(IMenuScreen screen) => _open.Add(screen);

    /// <summary>Closes <paramref name="screen"/> and any screen over it; closing the last one saves.</summary>
    public void Close(IMenuScreen screen)
    {
        int index = _open.IndexOf(screen);
        if (index < 0)
        {
            return;
        }

        // A capture is always the top screen, so closing any screen ends it.
        if (_open[^1] is CaptureScreen)
        {
            Capture = null;
        }

        _open.RemoveRange(index, _open.Count - index);
        if (_open.Count == 0)
        {
            _save();
        }
    }

    /// <summary>Closes every controls screen (the options menu closed around them) and saves.</summary>
    public void CloseAll()
    {
        if (IsOpen)
        {
            Close(_open[0]);
        }
    }

    public void Change(BindingProfile profile) => _profiles.Set(Device, profile);

    /// <summary>Puts <paramref name="device"/> back on its kind's shipped default and saves at once.</summary>
    public void ResetToDefault(InputDeviceInfo device)
    {
        _profiles.ResetToDefault(device);
        _save();
    }

    public void StartCapture(BindingLayer layer, string input, bool replace)
    {
        Capture = new ControlCapture(Device.Id, layer, input, replace, _now() + _timing.CaptureMilliseconds);
        Push(new CaptureScreen(this));
    }

    public void CancelCapture()
    {
        if (_open.Count > 0 && _open[^1] is CaptureScreen capture)
        {
            Close(capture);
        }
    }

    /// <summary>The seconds the capture still waits, rounded up.</summary>
    public int CaptureSecondsLeft() => Capture is { } capture ? (int)((Math.Max(0, capture.Deadline - _now()) + 999) / 1000) : 0;

    /// <summary>
    /// A device pressed <paramref name="control"/> while a capture waits; true when the capture took it (the press then
    /// does nothing else). A control other inputs of the layer use asks whether to move it or keep it on both.
    /// </summary>
    public bool Offer(InputDeviceId device, BoundControl control)
    {
        if (Capture is not { } capture || !capture.Accepts(device, control))
        {
            return false;
        }

        CancelCapture();
        List<string> others = Profile.InputsBoundTo(capture.Layer, control, except: capture.Input);
        if (others.Count == 0)
        {
            Bind(capture, control, onlyHere: false);
        }
        else
        {
            Push(new DuplicateBindingScreen(this, capture, control, others));
        }

        return true;
    }

    /// <param name="onlyHere">Takes the control off the layer's other inputs first.</param>
    public void Bind(ControlCapture capture, BoundControl control, bool onlyHere)
    {
        BindingProfile profile = Profile.Bind(capture.Layer, capture.Input, control, capture.Replace);
        Change(onlyHere ? profile.Release(capture.Layer, control, keepOn: capture.Input) : profile);
    }

    /// <summary>Gives up a capture whose time ran out; call once per displayed frame.</summary>
    public void Follow()
    {
        if (Capture is { } capture && _now() >= capture.Deadline)
        {
            CancelCapture();
        }
    }

    /// <summary>The test area: what the device gives now with its layout, raw and as the logic sees it.</summary>
    public MenuText[] TestLines()
    {
        InputWord raw = _tester.Read(Device.Id, Profile);
        return
        [
            MenuText.Key("MENU_CONTROLS_TEST", _tester.Describe(raw)),
            MenuText.Key("MENU_CONTROLS_TEST_LOGIC", _tester.Describe(_tester.Clean(raw))),
        ];
    }
}
