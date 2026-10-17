#if TOOLS
using FightingGame.Addons.CharacterEditor.Screen;
using FightingGame.Online;
using Godot;

namespace FightingGame.Addons.CharacterEditor;

/// <summary>
/// The character editor (ADR-0007): a main-screen tab of the Godot editor, next to 2D/3D/Script, that edits the
/// character JSON in <c>data/</c>. Startup code only; the screen does the work.
/// </summary>
[Tool]
public partial class CharacterEditorPlugin : EditorPlugin
{
    private CharacterEditorScreen? _screen;

    public override void _EnterTree()
    {
        _screen = new CharacterEditorScreen();
        // The editor and local matches read the same data folder, next to game/.
        _screen.Build(GameDataLocation.Choose().Folder);
        EditorInterface.Singleton.GetEditorMainScreen().AddChild(_screen);
        _MakeVisible(false);
    }

    public override void _ExitTree()
    {
        _screen?.QueueFree();
        _screen = null;
    }

    public override bool _HasMainScreen() => true;

    public override void _MakeVisible(bool visible)
    {
        if (_screen is not null)
        {
            _screen.Visible = visible;
        }
    }

    public override string _GetPluginName() => "Character";

    public override Texture2D _GetPluginIcon() =>
        EditorInterface.Singleton.GetEditorTheme().GetIcon("CharacterBody2D", "EditorIcons");

    // Asked when the editor closes, so unsaved character edits aren't lost silently.
    public override string _GetUnsavedStatus(string forScene) =>
        forScene.Length == 0 && _screen?.UnsavedCharacter is string id
            ? $"Character '{id}' has unsaved changes in the Character tab. Close anyway?"
            : "";
}
#endif
