#if TOOLS
using System;
using System.IO;
using FightingGame.Addons.ControlPanel.Catalog;
using FightingGame.Addons.ControlPanel.Documents;
using Godot;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>
/// The Characters page: the picked character's stats as cards, with a card of facts about the jumps they make,
/// worked out the way the logic flies them, so a gravity or jump speed change shows what it does before the match
/// restarts.
/// </summary>
[Tool]
public partial class CharacterPage : VBoxContainer
{
    private readonly CharacterPicker _picker = new();
    private readonly JumpCard _jumps = new();
    private readonly SettingsPage _settings = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
    private PanelDocument? _document;
    private string? _character;

    public CharacterPage()
    {
        AddChild(_settings);
        _settings.AddTop(_picker);
        _picker.Picked += Show;
    }

    public void Build(PanelDocument document, PanelLook look, SettingTooltips tooltips)
    {
        _document = document;
        _settings.Build(document, look, tooltips);
    }

    /// <inheritdoc cref="SettingsPage.SetHeading"/>
    public void SetHeading(string title, string description) => _settings.SetHeading(title, description);

    /// <summary>Lists the characters on disk again and shows the picked one.</summary>
    public void Fill(string dataRoot)
    {
        _picker.Fill(dataRoot, _character);
        if (_picker.Current is string id)
        {
            Show(id);
        }
    }

    public void Refresh()
    {
        _settings.Refresh();
        if (_character is null)
        {
            _jumps.ShowProblem("No characters in data/characters/.");
            return;
        }

        try
        {
            string id = _character;
            int gravity = (int)_document!.Number(CharacterStatSettingList.Gravity(id));
            int forward = (int)_document.Number(CharacterStatSettingList.JumpForwardSpeed(id));
            JumpArc jump = JumpArc.Of((int)_document.Number(CharacterStatSettingList.JumpVelocity(id)), gravity);
            JumpArc high = JumpArc.Of((int)_document.Number(CharacterStatSettingList.HighJumpVelocity(id)), gravity);
            _jumps.Show(jump, high, forward, (int)_document.Number(CharacterStatSettingList.PrejumpFrames(id)));
        }
        catch (Exception exception) when (exception is FormatException or IOException)
        {
            _jumps.ShowProblem($"No jump facts: {exception.Message}");
        }
    }

    private void Show(string characterId)
    {
        _character = characterId;
        _settings.Show(CharacterStatSettingList.For(characterId), [_jumps]);
        Refresh();
    }
}
#endif
