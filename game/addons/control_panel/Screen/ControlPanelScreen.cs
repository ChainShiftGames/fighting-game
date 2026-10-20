#if TOOLS
using System.Collections.Generic;
using System.IO;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using FightingGame.Addons.ControlPanel.Catalog;
using FightingGame.Addons.ControlPanel.Documents;
using Godot;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>
/// The developer control panel's main screen (developer settings, ARCHITECTURE.md §6): pages for the mechanics
/// modules, the match rules, character stats, the hit feel of every move and the effects. Edits stay in memory until
/// saved; every save first passes the game's own loaders.
/// </summary>
/// <remarks>
/// Styled like the Character tab (its <see cref="MapTheme"/>, page switch and Save button), so the two read as one tool.
/// With Apply live on, each change is saved once it has settled and passes; a game run from the editor sees the
/// files change and restarts its local or training match with them (P3-43).
/// </remarks>
[Tool]
public partial class ControlPanelScreen : VBoxContainer
{
    // Long enough that a dragged slider or typed number saves once, not per step.
    private const double SettleSeconds = 0.35;
    private const string MetadataSection = "control_panel";
    private const string LiveKey = "apply_live";

    private readonly PanelTopBar _topBar = new();
    private static readonly string[] _pageNames = ["Mechanics", "Match", "Characters", "Hit feel", "Effects"];

    private static readonly string[] _pageTips =
    [
        "Switch each mechanic on or off and tune its values (data/rules/modules/).",
        "Round timer, rounds, stage size, counter hits, throws, input windows and rollback (data/rules/match.json).",
        "Health, walking, jumping and gravity of one character (its character.json).",
        "Damage, hitstop, hitstun, blockstun and pushback of every hit of one character's moves, in one table.",
        "Screen shake, hit spark strength and size, controller rumble (game/Assets/Effects/effect_settings.json).",
    ];

    private readonly PanelContainer _body = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
    private readonly SettingsPage _mechanics = new();
    private readonly SettingsPage _match = new();
    private readonly CharacterPage _characters = new();
    private readonly HitFeelPage _hitFeel = new();
    private readonly SettingsPage _effects = new();
    private readonly Control[] _pages;
    private readonly PanelContainer _problemCard = new() { ThemeTypeVariation = MapTheme.Card, Visible = false };
    private readonly Label _problems = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    private readonly Timer _settle = new() { OneShot = true, WaitTime = SettleSeconds };
    private PanelDocument? _document;
    private PanelSaver? _saver;
    private PanelLook? _look;
    private SettingTooltips? _tooltips;
    private string _lastSave = string.Empty;
    private bool _live;
    private bool _checked;
    private int _page;
    private int _problemCount;

    public ControlPanelScreen()
    {
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeConstantOverride("separation", 0);
        _pages = [_mechanics, _match, _characters, _hitFeel, _effects];
        _topBar.ShowPages(_pageNames, _pageTips);
        var stack = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        foreach (Control page in _pages)
        {
            page.SizeFlagsVertical = SizeFlags.ExpandFill;
            stack.AddChild(page);
        }

        _problemCard.AddChild(_problems);
        stack.AddChild(_problemCard);
        _body.AddChild(stack);
        foreach (Node child in new Node[] { _topBar, _body, _settle })
        {
            AddChild(child);
        }

        _topBar.SavePressed += () => Save();
        _topBar.RevertPressed += () => _document?.RevertAll();
        _topBar.ReloadPressed += Reload;
        _topBar.LiveToggled += SetLive;
        _topBar.ProblemsPressed += () => _problemCard.Visible = !_problemCard.Visible && _problems.Text.Length > 0;
        _topBar.PageChosen += OpenPage;
        _settle.Timeout += OnSettled;
        OpenPage(0);
    }

    /// <summary>True while there are edits not on disk.</summary>
    public bool HasUnsavedChanges => _document?.HasChanges == true;

    /// <summary>The unsaved edits as text, for keeping them across a hot reload (<see cref="PanelEditStash"/>).</summary>
    public Dictionary<string, string> TakeEdits() => _document is null ? [] : PanelEditStash.Take(_document);

    /// <summary>Puts edits taken before a hot reload back, so a code rebuild doesn't lose tuning in progress.</summary>
    public void RestoreEdits(IReadOnlyDictionary<string, string> edits)
    {
        if (_document is not null && edits.Count > 0)
        {
            PanelEditStash.Restore(_document, edits);
        }
    }

    /// <param name="dataRoot">Full path of <c>data/</c>; null when there is none (nothing to edit).</param>
    /// <param name="gameRoot">Full path of the Godot project, <c>game/</c>.</param>
    public void Build(string? dataRoot, string gameRoot)
    {
        _look = PanelLook.FromEditor();
        Theme = _look.Theme;
        Style(_look);
        if (dataRoot is null)
        {
            _topBar.Disable("No data/ folder next to game/: nothing to edit.");
            _body.Visible = false;
            return;
        }

        _document = new PanelDocument(new PanelFolders(dataRoot, gameRoot));
        _saver = new PanelSaver(_document, [new GameDataCheck(dataRoot), new EffectSettingsCheck()]);
        _document.Changed += OnChanged;
        _tooltips = new SettingTooltips(_document);
        _mechanics.Build(_document, _look, _tooltips);
        _match.Build(_document, _look, _tooltips);
        _characters.Build(_document, _look, _tooltips);
        _hitFeel.Build(_document, _look, _tooltips);
        _effects.Build(_document, _look, _tooltips);
        _mechanics.SetHeading(_pageNames[0], _pageTips[0]);
        _match.SetHeading(_pageNames[1], _pageTips[1]);
        _characters.SetHeading(_pageNames[2], _pageTips[2]);
        _hitFeel.SetHeading(_pageNames[3], _pageTips[3]);
        _effects.SetHeading(_pageNames[4], _pageTips[4]);
        _problems.TooltipText = "Why the last save or check failed. Nothing was written.";
        _problems.MouseFilter = MouseFilterEnum.Stop;
        _live = (bool)EditorInterface.Singleton.GetEditorSettings().GetProjectMetadata(MetadataSection, LiveKey, false);
        _topBar.ShowLive(_live);
        Fill();
    }

    /// <summary>Called when the tab is shown: picks up files another editor saved, unless there are unsaved edits.</summary>
    public void OnShown()
    {
        if (_document is { HasChanges: false })
        {
            _document.Reload();
            Fill();
        }
    }

    /// <summary>Saves the changes when they pass the checks; problems are shown under the pages.</summary>
    /// <returns>True when everything was saved.</returns>
    public bool Save()
    {
        if (_document is null || _saver is null || !_document.HasChanges)
        {
            return true;
        }

        int files = _document.ChangedFiles.Count;
        try
        {
            IReadOnlyList<string> problems = _saver.Save();
            _checked = true;
            ShowProblems(problems);
            _lastSave = problems.Count == 0
                ? $"Saved {files} file{(files == 1 ? "" : "s")}; a running game restarts its match"
                : string.Empty;
            return problems.Count == 0;
        }
        catch (IOException exception)
        {
            ShowProblems([exception.Message]);
            return false;
        }
        finally
        {
            ShowStatus();
        }
    }

    // The pages on the deep background, as the Character tab's map.
    private void Style(PanelLook look)
    {
        var body = new StyleBoxFlat { BgColor = MapPalette.Background };
        body.SetContentMarginAll(look.Px(18f));
        _body.AddThemeStyleboxOverride("panel", body);
        _problems.AddThemeColorOverride("font_color", look.Problem);
    }

    private void OpenPage(int index)
    {
        _page = index;
        for (int page = 0; page < _pages.Length; page++)
        {
            _pages[page].Visible = page == index;
        }

        RefreshPage();
    }

    private void Fill()
    {
        string dataRoot = _document!.Folders.DataRoot;
        _mechanics.Show(MechanicSettingList.For(path => File.Exists(Path.Combine(dataRoot, path))));
        _match.Show(MatchRuleSettingList.All());
        _characters.Fill(dataRoot);
        _hitFeel.Fill(dataRoot);
        _effects.Show(EffectSettingList.All());
        _checked = false;
        ShowProblems([]);
        ShowStatus();
    }

    private void Reload()
    {
        _document?.Reload();
        if (_document is not null)
        {
            Fill();
        }
    }

    private void SetLive(bool live)
    {
        _live = live;
        EditorInterface.Singleton.GetEditorSettings().SetProjectMetadata(MetadataSection, LiveKey, live);
        if (live && HasUnsavedChanges)
        {
            _settle.Start();
        }
    }

    private void OnChanged()
    {
        _lastSave = string.Empty;
        _checked = false;
        RefreshPage();
        ShowStatus();
        if (HasUnsavedChanges)
        {
            _settle.Start();
        }
    }

    // Checks run once edits settle: loading all game data on every slider step would make dragging stutter.
    private void OnSettled()
    {
        if (_live)
        {
            Save();
        }
        else if (_saver is not null)
        {
            _checked = true;
            ShowProblems(_saver.Check());
        }
    }

    private void RefreshPage()
    {
        switch (_pages[_page])
        {
            case SettingsPage page:
                page.Refresh();
                break;
            case CharacterPage page:
                page.Refresh();
                break;
            case HitFeelPage page:
                page.Refresh();
                break;
        }
    }

    private void ShowStatus()
    {
        if (_document is null)
        {
            return;
        }

        var files = new List<string>();
        foreach (PanelFile file in _document.ChangedFiles.Keys)
        {
            files.Add(file.ToString());
        }

        string status = files.Count > 0 ? $"{files.Count} file{(files.Count == 1 ? "" : "s")} not saved yet"
            : _lastSave.Length > 0 ? _lastSave
            : "All saved";
        string tooltip = files.Count > 0 ? string.Join("\n", files)
            : "Developer settings: shipped builds use the values saved in these files.";
        _topBar.ShowStatus(status, tooltip, files.Count > 0);
        _topBar.ShowProblems(_problemCount, files.Count > 0, _checked);
    }

    private void ShowProblems(IReadOnlyList<string> problems)
    {
        _problems.Text = problems.Count == 0 ? string.Empty : "Not saved:\n" + string.Join("\n", problems);
        _problemCard.Visible = problems.Count > 0;
        _problemCount = problems.Count;
        _topBar.ShowProblems(problems.Count, HasUnsavedChanges, _checked);
    }
}
#endif
