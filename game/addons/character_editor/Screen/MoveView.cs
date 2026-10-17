#if TOOLS
using FightingGame.Addons.CharacterEditor.Documents;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen;

/// <summary>
/// The selected move's file, read-only, laid out as saving would write it. The move and frame data editors
/// (P3-41) and the box editor (P3-42) take this place in the screen.
/// </summary>
[Tool]
public partial class MoveView : VBoxContainer
{
    private readonly Label _path = new();
    private readonly TextEdit _json = new()
    {
        Editable = false,
        SizeFlagsVertical = SizeFlags.ExpandFill,
        WrapMode = TextEdit.LineWrappingMode.None,
    };

    public MoveView()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(_path);
        AddChild(_json);
        ShowMove(null);
    }

    /// <summary>Shows the move's file; nothing when null (no move selected, or its file is missing).</summary>
    public void ShowMove(EditedFile? move)
    {
        _path.Text = move?.Path ?? "No move selected";
        _json.Text = move is null ? "" : JsonDataWriter.Write(move.Root);
    }
}
#endif
