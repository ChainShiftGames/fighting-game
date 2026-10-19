using System;
using System.Collections.Generic;
using Godot;

namespace FightingGame.Debug;

/// <summary>
/// Debug panel rows for the characters' art-file look settings (developer settings, ADR-0028). Save writes them to
/// the art files, versioned with the art, and the host restarts the match, which reads its art files anew.
/// </summary>
public partial class FighterLookControls : VBoxContainer
{
    private readonly List<(DataFileSetting Setting, SpinBox Box)> _rows = [];
    private readonly Label _status = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private string _root = string.Empty;

    /// <summary>Raised after Save wrote the art files, so the host can restart the match with them.</summary>
    public event Action? Saved;

    /// <param name="charactersRoot">The art files' folder on disk (<c>res://Assets/Characters</c>).</param>
    public void Build(IReadOnlyList<DataFileSetting> settings, string charactersRoot)
    {
        _root = charactersRoot;
        foreach (DataFileSetting setting in settings)
        {
            // Step 1 with a custom arrow step, as the panel's data rows do, so typed values aren't snapped.
            var box = new SpinBox { MinValue = setting.Min, MaxValue = setting.Max, Step = 1, CustomArrowStep = setting.Step };
            box.GetLineEdit().FocusMode = FocusModeEnum.Click;
            box.SetValueNoSignal(setting.Read(charactersRoot));
            _rows.Add((setting, box));
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = setting.Name, SizeFlagsHorizontal = SizeFlags.ExpandFill });
            row.AddChild(box);
            AddChild(row);
        }

        var save = new Button { Text = "Save to art files and restart match", FocusMode = FocusModeEnum.None };
        save.Pressed += Save;
        AddChild(save);
        AddChild(_status);
    }

    private void Save()
    {
        try
        {
            foreach ((DataFileSetting setting, SpinBox box) in _rows)
            {
                setting.Write(_root, (int)box.Value);
            }
        }
        catch (Exception exception) when (exception is System.IO.IOException or FormatException or ArgumentOutOfRangeException)
        {
            _status.Text = exception.Message;
            return;
        }

        _status.Text = string.Empty;
        GetViewport()?.GuiReleaseFocus();
        Saved?.Invoke();
    }
}
