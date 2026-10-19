using System;
using System.Collections.Generic;
using System.IO;
using Godot;

namespace FightingGame.Debug;

/// <summary>
/// The debug panel (developer setting): lists every developer setting, scrolling when they outgrow the window.
/// Toggles act at once; values stored in <c>data/</c> are written back to their files on Save, and the host then
/// restarts the local match with them.
/// </summary>
/// <remarks>
/// A new developer setting is one more <see cref="DeveloperToggle"/>, <see cref="DataFileFlag"/>,
/// <see cref="DataFileSetting"/> or <see cref="DataFileChoice"/> in the lists the host passes in, or a section
/// (<see cref="AddSection"/>); the panel needs no change. Its controls never take keyboard focus by themselves, so the
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
    private readonly List<(DataFileFlag Setting, CheckBox Box)> _flagRows = [];
    private readonly List<(DataFileSetting Setting, SpinBox Box)> _dataRows = [];
    private readonly List<(DataFileChoice Setting, OptionButton Box)> _choiceRows = [];
    private string? _dataRoot;
    private int _sectionsEnd;

    /// <summary>Raised after Save wrote the data files, so the host can restart the match with them.</summary>
    public event Action? DataSaved;

    /// <param name="dataRoot">The data folder the settings are stored in; null when the game has none.</param>
    /// <param name="keys">One line listing the developer keys.</param>
    public void Build(
        IReadOnlyList<DeveloperToggle> toggles,
        IReadOnlyList<DataFileFlag> dataFlags,
        IReadOnlyList<DataFileSetting> dataSettings,
        IReadOnlyList<DataFileChoice> dataChoices,
        string? dataRoot,
        string keys)
    {
        _dataRoot = dataRoot;
        _rows.AddThemeConstantOverride("separation", Spacing);
        _rows.AddChild(new Label { Text = "Developer settings" });
        _rows.AddChild(new Label { Text = keys, AutowrapMode = TextServer.AutowrapMode.WordSmart });
        foreach (DeveloperToggle toggle in toggles)
        {
            AddToggleRow(toggle);
        }

        _sectionsEnd = _rows.GetChildCount();
        _rows.AddChild(new HSeparator());
        _rows.AddChild(new Label { Text = dataRoot is null ? "data/ (embedded in this build: read-only)" : "data/ (saved to the files)" });
        foreach (DataFileFlag setting in dataFlags)
        {
            AddFlagRow(setting);
        }

        foreach (DataFileSetting setting in dataSettings)
        {
            AddDataRow(setting);
        }

        foreach (DataFileChoice setting in dataChoices)
        {
            AddChoiceRow(setting);
        }

        var save = new Button { Text = "Save to data/ and restart match", FocusMode = Control.FocusModeEnum.None };
        save.Disabled = dataRoot is null;
        save.Pressed += Save;
        _rows.AddChild(save);
        _rows.AddChild(_status);
    }

    /// <summary>
    /// Adds a titled group of developer controls that act at once (e.g. the network simulator), below the toggles
    /// and above the data files, which only apply on Save.
    /// </summary>
    public void AddSection(string title, Control section)
    {
        foreach (Control row in new Control[] { new HSeparator(), new Label { Text = title }, section })
        {
            _rows.AddChild(row);
            _rows.MoveChild(row, _sectionsEnd++);
        }
    }

    public override void _Ready()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        panel.GrowHorizontal = Control.GrowDirection.Begin;
        panel.OffsetRight = -Margin;
        panel.OffsetTop = TopOffset;

        // The settings outgrow the window, so the panel reaches down to the bottom margin and its rows scroll.
        panel.AnchorBottom = 1f;
        panel.OffsetBottom = -Margin;
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FocusMode = Control.FocusModeEnum.None,
        };
        scroll.AddChild(_rows);
        panel.AddChild(scroll);
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
        string text = toggle.Key == Key.None ? toggle.Name : $"{toggle.Name} ({toggle.Key})";
        var box = new CheckBox { Text = text, FocusMode = Control.FocusModeEnum.None };
        box.Toggled += pressed => toggle.Enabled = pressed;
        _toggleRows.Add((toggle, box));
        _rows.AddChild(box);
    }

    private void AddFlagRow(DataFileFlag setting)
    {
        var box = new CheckBox
        {
            Text = $"{setting.Name} ({setting.File})",
            FocusMode = Control.FocusModeEnum.None,
            Disabled = _dataRoot is null,
        };
        _flagRows.Add((setting, box));
        _rows.AddChild(box);
    }

    // Step 1 with a custom arrow step: Godot snaps a value to min + n * step, so a coarse step would move values
    // the loader accepts (wallDistance's min is 1, its arrows move a pixel).
    private void AddDataRow(DataFileSetting setting)
    {
        var box = new SpinBox
        {
            MinValue = setting.Min,
            MaxValue = setting.Max,
            Step = 1,
            CustomArrowStep = setting.Step,
            Editable = _dataRoot is not null,
        };
        box.GetLineEdit().FocusMode = Control.FocusModeEnum.Click;
        _dataRows.Add((setting, box));
        AddLabelledRow($"{setting.Name} ({setting.File})", box);
    }

    private void AddChoiceRow(DataFileChoice setting)
    {
        var box = new OptionButton { FocusMode = Control.FocusModeEnum.None, Disabled = _dataRoot is null };
        foreach (string choice in setting.Choices)
        {
            box.AddItem(choice);
        }

        _choiceRows.Add((setting, box));
        AddLabelledRow($"{setting.Name} ({setting.File})", box);
    }

    private void AddLabelledRow(string text, Control box)
    {
        var row = new HBoxContainer();
        row.AddChild(new Label { Text = text, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
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
            foreach ((DataFileFlag setting, CheckBox box) in _flagRows)
            {
                box.SetPressedNoSignal(setting.Read(_dataRoot));
            }

            foreach ((DataFileSetting setting, SpinBox box) in _dataRows)
            {
                box.SetValueNoSignal(setting.Read(_dataRoot));
            }

            foreach ((DataFileChoice setting, OptionButton box) in _choiceRows)
            {
                box.Select(setting.IndexOf(setting.Read(_dataRoot)));
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
            foreach ((DataFileFlag setting, CheckBox box) in _flagRows)
            {
                if (box.ButtonPressed != setting.Read(_dataRoot))
                {
                    setting.Write(_dataRoot, box.ButtonPressed);
                    written++;
                }
            }

            foreach ((DataFileSetting setting, SpinBox box) in _dataRows)
            {
                int value = (int)box.Value;
                if (value != setting.Read(_dataRoot))
                {
                    setting.Write(_dataRoot, value);
                    written++;
                }
            }

            foreach ((DataFileChoice setting, OptionButton box) in _choiceRows)
            {
                string choice = setting.Choices[box.Selected];
                if (choice != setting.Read(_dataRoot))
                {
                    setting.Write(_dataRoot, choice);
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
