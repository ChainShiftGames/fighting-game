#if TOOLS
using System;
using System.Collections.Generic;
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// Edits <c>character.json</c> through its schema: display name, stats, the basic move for each state, and any
/// field a later schema adds (such as a module's per-character data). The move list has its own panel.
/// </summary>
[Tool]
public partial class CharacterPanel : VBoxContainer
{
    private static readonly HashSet<string> _notInForm = ["$schema", "schemaVersion", "id", "moves"];

    private readonly Label _path = new();
    private readonly SchemaForm _form = new();
    private EditedFile? _character;

    public CharacterPanel()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(_path);
        AddChild(_form);
        _form.Changed += () =>
        {
            _character!.MarkChanged();
            Edited?.Invoke();
        };
    }

    public event Action? Edited;

    /// <summary>Shows the character file; nothing when null (no character open).</summary>
    public void ShowCharacter(EditedFile? character, EditingContext? context)
    {
        _character = character;
        _path.Text = character?.Path ?? "No character open";
        _form.Visible = character is not null && context is not null;
        if (character is not null && context is not null)
        {
            _form.Show(character.Root, context.CharacterSchema, field => !_notInForm.Contains(field));
        }
    }
}
#endif
