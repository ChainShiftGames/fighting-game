#if TOOLS
using FightingGame.Addons.ControlPanel.Screen;
using FightingGame.Online;
using Godot;

namespace FightingGame.Addons.ControlPanel;

/// <summary>
/// The developer control panel: a main-screen tab of the Godot editor, next to the Character tab, that toggles the
/// mechanics and tunes the match rules, character stats, hit feel and effects. Startup code only; the screen does the
/// work.
/// </summary>
[Tool]
public partial class ControlPanelPlugin : EditorPlugin
{
    private const string ScreenName = "ControlPanelScreen";

    private ControlPanelScreen? _screen;

    public override void _EnterTree()
    {
        // A C# rebuild sets the plugin up again but can leave the old screen in the editor; drop it so there is one.
        Control mainScreen = EditorInterface.Singleton.GetEditorMainScreen();
        mainScreen.GetNodeOrNull(ScreenName)?.Free();
        _screen = new ControlPanelScreen { Name = ScreenName };
        _screen.Build(GameDataLocation.Choose().Folder, ProjectSettings.GlobalizePath("res://"));
        mainScreen.AddChild(_screen);
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
        if (_screen is null)
        {
            return;
        }

        _screen.Visible = visible;
        if (visible)
        {
            _screen.OnShown();
        }
    }

    public override string _GetPluginName() => "Control Panel";

    public override Texture2D _GetPluginIcon() =>
        EditorInterface.Singleton.GetEditorTheme().GetIcon("EditorPlugin", "EditorIcons");

    // Ctrl+S and Save All save the panel's edits too.
    public override void _SaveExternalData() => _screen?.Save();

    // Asked when the editor closes, so unsaved settings aren't lost silently.
    public override string _GetUnsavedStatus(string forScene) =>
        forScene.Length == 0 && _screen?.HasUnsavedChanges == true
            ? "The Control Panel has unsaved settings. Close anyway?"
            : "";
}
#endif
