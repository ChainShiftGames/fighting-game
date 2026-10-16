using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace FightingGame.Debug;

/// <summary>
/// The debug panel (developer setting): lists every developer setting. Toggles act at once; values stored in
/// <c>data/</c> are written back to their files on Save, and the host then restarts the local match with them.
/// </summary>
/// <remarks>
/// A new developer setting is one more <see cref="DeveloperToggle"/> or <see cref="DataFileSetting"/> in the lists
/// the host passes in; the panel needs no change. Its controls never take keyboard focus by themselves, so the
/// players' keys keep reaching the match while it is open.
/// </remarks>
public partial class DebugPanel : CanvasLayer
{
    private const float Margin = 12f;
    private const float TopOffset = 96f;
    private const int Spacing = 6;

    private readonly VBoxContainer _rows = new() { CustomMinimumSize = new Vector2(320f, 0f) };
    private readonly Label _status = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly List<(DeveloperToggle Toggle, CheckBox Box)> _toggleRows = [];
    private readonly List<(DataFileSetting Setting, SpinBox Box)> _dataRows = [];
    private string? _dataRoot;

    /// <summary>Raised after Save wrote the data files, so the host can restart the match with them.</summary>
    public event Action? DataSaved;

    /// <param name="dataRoot">The data folder the settings are stored in; null when the game has none.</param>
    /// <param name="keys">One line listing the developer keys.</param>
    public void Build(
        IReadOnlyList<DeveloperToggle> toggles, IReadOnlyList<DataFileSetting> dataSettings, string? dataRoot, string keys)
    {
        _dataRoot = dataRoot;
        _rows.AddThemeConstantOverride("separation", Spacing);
        _rows.AddChild(new Label { Text = "Developer settings" });
        _rows.AddChild(new Label { Text = keys, AutowrapMode = TextServer.AutowrapMode.WordSmart });
        foreach (DeveloperToggle toggle in toggles)
        {
            AddToggleRow(toggle);
        }

        _rows.AddChild(new HSeparator());
        _rows.AddChild(new Label { Text = dataRoot is null ? "data/ (embedded in this build: read-only)" : "data/ (saved to the files)" });
        foreach (DataFileSetting setting in dataSettings)
        {
            AddDataRow(setting);
        }

        var save = new Button { Text = "Save to data/ and restart match", FocusMode = Control.FocusModeEnum.None };
        save.Disabled = dataRoot is null;
        save.Pressed += Save;
        _rows.AddChild(save);
        _rows.AddChild(_status);
    }

    public override void _Ready()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        panel.GrowHorizontal = Control.GrowDirection.Begin;
        panel.OffsetRight = -Margin;
        panel.OffsetTop = TopOffset;
        panel.AddChild(_rows);
        AddChild(panel);
    }

    /// <summary>Shows the panel with the values currently in the files, or hides it.</summary>
    public void Toggle()
    {
        Visible = !Visible;
        if (Visible)
        {
            ReadDataValues();
        }
        else
        {
            _rows.GetViewport()?.GuiReleaseFocus();
        }
    }

    // Keys flip toggles too, so the boxes follow them.
    public override void _Process(double delta)
    {
        foreach ((DeveloperToggle toggle, CheckBox box) in _toggleRows)
        {
            box.SetPressedNoSignal(toggle.Enabled);
        }
    }

    private void AddToggleRow(DeveloperToggle toggle)
    {
        var box = new CheckBox { Text = $"{toggle.Name} ({toggle.Key})", FocusMode = Control.FocusModeEnum.None };
        box.Toggled += pressed => toggle.Enabled = pressed;
        _toggleRows.Add((toggle, box));
        _rows.AddChild(box);
    }

    private void AddDataRow(DataFileSetting setting)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label
        {
            Text = $"{setting.Name} ({setting.File})",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        });
        var box = new SpinBox
        {
            MinValue = setting.Min,
            MaxValue = setting.Max,
            Step = setting.Step,
            Editable = _dataRoot is not null,
        };
        box.GetLineEdit().FocusMode = Control.FocusModeEnum.Click;
        _dataRows.Add((setting, box));
        row.AddChild(box);
        _rows.AddChild(row);
    }

    private void ReadDataValues()
    {
        if (_dataRoot is null)
        {
            return;
        }

        try
        {
            foreach ((DataFileSetting setting, SpinBox box) in _dataRows)
            {
                box.SetValueNoSignal(setting.Read(_dataRoot));
            }

            _status.Text = string.Empty;
        }
        catch (Exception exception) when (exception is IOException or FormatException)
        {
            _status.Text = exception.Message;
        }
    }

    // Writes only the values that changed, so an untouched file keeps its exact bytes.
    private void Save()
    {
        if (_dataRoot is null)
        {
            return;
        }

        try
        {
            int written = 0;
            foreach ((DataFileSetting setting, SpinBox box) in _dataRows)
            {
                int value = (int)box.Value;
                if (value != setting.Read(_dataRoot))
                {
                    setting.Write(_dataRoot, value);
                    written++;
                }
            }

            _status.Text = $"Saved {written} value(s); the match restarted.";
            _rows.GetViewport()?.GuiReleaseFocus();
            DataSaved?.Invoke();
        }
        catch (Exception exception) when (exception is IOException or FormatException or ArgumentOutOfRangeException)
        {
            _status.Text = exception.Message;
        }
    }
}
