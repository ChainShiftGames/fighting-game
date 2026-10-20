#if TOOLS
using System.Collections.Generic;
using System.Runtime.Loader;
using FightingGame.Addons.ControlPanel.Screen;
using FightingGame.Online;
using Godot;

namespace FightingGame.Addons.ControlPanel;

/// <summary>
/// The developer control panel: a main-screen tab of the Godot editor, next to the Character tab, that toggles the
/// mechanics and tunes the match rules, character stats, hit feel and effects. Startup code only; the screen does the
/// work.
/// </summary>
/// <remarks>
/// A C# hot reload keeps the editor's nodes but creates their C# objects again by running each constructor, which
/// adds a second copy of every control, and doesn't run <see cref="_EnterTree"/>. So after a reload the plugin drops
/// the old screen and builds a new one, as visible as the old; unsaved edits wait on the editor's main screen node
/// (which outlives the assembly) while the old assembly unloads.
/// </remarks>
[Tool]
public partial class ControlPanelPlugin : EditorPlugin
{
    private const string ScreenName = "ControlPanelScreen";
    private const string StashKey = "control_panel_unsaved_edits";

    // Static state starts over with every assembly load: false in a plugin object a hot reload created.
    private static bool _screenBuiltThisLoad;

    private ControlPanelScreen? _screen;

    public ControlPanelPlugin()
    {
        // After _EnterTree on a normal start; the only setup a hot-reloaded plugin gets.
        Callable.From(RebuildAfterReload).CallDeferred();
    }

    public override void _EnterTree()
    {
        BuildScreen(visible: false);
        AssemblyLoadContext.GetLoadContext(typeof(ControlPanelPlugin).Assembly)!.Unloading += StashEdits;
    }

    public override void _ExitTree()
    {
        AssemblyLoadContext.GetLoadContext(typeof(ControlPanelPlugin).Assembly)!.Unloading -= StashEdits;
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

    private void RebuildAfterReload()
    {
        if (_screenBuiltThisLoad || !IsInsideTree())
        {
            return;
        }

        bool visible = EditorInterface.Singleton.GetEditorMainScreen().GetNodeOrNull<Control>(ScreenName)?.Visible ?? false;
        BuildScreen(visible);
        AssemblyLoadContext.GetLoadContext(typeof(ControlPanelPlugin).Assembly)!.Unloading += StashEdits;
    }

    // Replaces any screen already there (an older load's, possibly duplicated by a reload) with a new one.
    private void BuildScreen(bool visible)
    {
        Control mainScreen = EditorInterface.Singleton.GetEditorMainScreen();
        mainScreen.GetNodeOrNull(ScreenName)?.Free();
        _screen = new ControlPanelScreen { Name = ScreenName };
        _screen.Build(GameDataLocation.Choose().Folder, ProjectSettings.GlobalizePath("res://"));
        mainScreen.AddChild(_screen);
        if (mainScreen.HasMeta(StashKey))
        {
            _screen.RestoreEdits(mainScreen.GetMeta(StashKey).AsGodotDictionary<string, string>());
            mainScreen.RemoveMeta(StashKey);
        }

        _screenBuiltThisLoad = true;
        _MakeVisible(visible);
    }

    // Runs as the old assembly unloads, while this object and its screen still work. Whatever goes wrong here must
    // not stop the reload, so it only warns: the cost is the unsaved edits.
    private void StashEdits(AssemblyLoadContext context)
    {
        try
        {
            if (_screen is null || !IsInstanceValid(_screen) || !_screen.HasUnsavedChanges)
            {
                return;
            }

            var edits = new Godot.Collections.Dictionary<string, string>();
            foreach ((string key, string text) in _screen.TakeEdits())
            {
                edits[key] = text;
            }

            EditorInterface.Singleton.GetEditorMainScreen().SetMeta(StashKey, edits);
        }
        catch (System.Exception exception)
        {
            GD.PushWarning($"Control Panel: unsaved edits not kept across the reload: {exception.Message}");
        }
    }
}
#endif
