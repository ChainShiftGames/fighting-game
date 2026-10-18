using System;
using System.Collections.Generic;
using FightingGame.VfxLab.Files;
using Godot;

namespace FightingGame.VfxLab.Panels;

/// <summary>The Lab's top line: which sheet, its title, the brief (folded open on demand) and the save/reload status.</summary>
public partial class LabHeaderBar : VBoxContainer
{
    private readonly Label _status = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill, HorizontalAlignment = HorizontalAlignment.Right, Modulate = new Color(1f, 1f, 1f, 0.6f) };
    private readonly RichTextLabel _brief = new() { FitContent = true, Visible = false, BbcodeEnabled = true, SelectionEnabled = true };

    /// <summary>Call once per sheet, before the bar joins the scene.</summary>
    public void Build(IReadOnlyList<string> sheets, string current, LabSheet? sheet, Action<string> sheetChosen)
    {
        HBoxContainer line = new();
        int selected = Math.Max(0, IndexOf(sheets, current));
        line.AddChild(LabChoice.Of("Which effect sheet", [.. sheets], selected, index => sheetChosen(sheets[index])));
        Label title = new() { Text = sheet?.Title ?? "No sheet" };
        title.AddThemeFontSizeOverride("font_size", 22);
        line.AddChild(title);
        Button brief = new() { Text = "Brief", ToggleMode = true, TooltipText = "What the effect is for and the character's style" };
        brief.Toggled += on => _brief.Visible = on;
        line.AddChild(brief);
        line.AddChild(_status);
        AddChild(line);
        if (sheet is not null)
        {
            _brief.Text = BriefText(sheet);
        }

        AddChild(_brief);
    }

    /// <summary>Shows a status line; <paramref name="problem"/> shows it in red.</summary>
    public void ShowStatus(string text, bool problem = false)
    {
        _status.Text = text;
        _status.Modulate = problem ? new Color(1f, 0.45f, 0.45f) : new Color(1f, 1f, 1f, 0.6f);
        _status.TooltipText = text;
    }

    private static string BriefText(LabSheet sheet)
    {
        List<string> lines = [$"[b]Brief[/b]  {sheet.Brief}", $"[b]Style ({sheet.Style.Character})[/b]  {sheet.Style.Motifs}"];
        foreach (LabRound round in sheet.Rounds)
        {
            lines.Add($"[b]{round.Name}[/b]  {round.Note}");
        }

        return string.Join("\n", lines);
    }

    private static int IndexOf(IReadOnlyList<string> items, string item)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == item)
            {
                return i;
            }
        }

        return -1;
    }
}
