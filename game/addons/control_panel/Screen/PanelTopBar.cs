#if TOOLS
using System;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.ControlPanel.Screen;

/// <summary>
/// The control panel's top bar, laid out like the Character tab's: the title, the page switch (like its lenses), the
/// status, a chip that says whether the changes pass the checks (click for the problems), Apply live, a menu (revert,
/// reload) and Save.
/// </summary>
[Tool]
public partial class PanelTopBar : PanelContainer
{
    private const int RevertId = 0;
    private const int ReloadId = 1;

    private readonly Label _title = new() { Text = "Control Panel", ThemeTypeVariation = MapTheme.Title };
    private readonly Label _subtitle = new() { Text = "· developer settings", ThemeTypeVariation = MapTheme.Title };
    private readonly Label _status = new()
    {
        ThemeTypeVariation = MapTheme.MutedLabel,
        TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        SizeFlagsHorizontal = SizeFlags.ExpandFill,
        HorizontalAlignment = HorizontalAlignment.Right,
        MouseFilter = MouseFilterEnum.Stop,
    };

    private readonly Button _chip = new() { ThemeTypeVariation = MapTheme.Chip, FocusMode = FocusModeEnum.None };
    private readonly CheckButton _live = new() { Text = "Apply live", FocusMode = FocusModeEnum.None };
    private readonly MenuButton _menu = new() { Text = "•••", TooltipText = "More: revert all changes, reload from disk" };
    private readonly GradientButton _save = new()
    {
        Text = "Save",
        TooltipText = "Check the changes with the game's own loaders and write them (also Ctrl+S).",
    };

    private readonly HBoxContainer _row = new();
    private readonly HBoxContainer _pages = new();
    private readonly ButtonGroup _pageGroup = new();

    public PanelTopBar()
    {
        _title.TooltipText = "Developer settings for the whole game. Shipped builds use the values saved here.";
        _title.MouseFilter = MouseFilterEnum.Stop;
        _subtitle.AddThemeColorOverride("font_color", MapPalette.Muted);
        _live.TooltipText =
            "Save each change as soon as it passes the checks. A game running from the editor restarts its match with it.";
        _chip.TooltipText = "Whether the changes pass the game's checks. Click to show or hide the problems.";
        var title = new HBoxContainer();
        title.AddThemeConstantOverride("separation", 2);
        title.AddChild(_title);
        title.AddChild(_subtitle);
        _row.AddThemeConstantOverride("separation", 16);
        foreach (Control control in new Control[] { title, PageFrame(), _status, _chip, _live, _menu, _save })
        {
            _row.AddChild(control);
        }

        AddChild(_row);
        PopupMenu menu = _menu.GetPopup();
        menu.AddItem("Revert all unsaved changes", RevertId);
        menu.AddItem("Reload from disk (after the Character tab or git changed files)", ReloadId);
        menu.IdPressed += id => (id == RevertId ? RevertPressed : ReloadPressed)?.Invoke();
        _save.Pressed += () => SavePressed?.Invoke();
        _live.Toggled += pressed => LiveToggled?.Invoke(pressed);
        _chip.Pressed += () => ProblemsPressed?.Invoke();
    }

    public event Action? SavePressed;

    /// <summary>A page was picked in the switch; its index.</summary>
    public event Action<int>? PageChosen;

    public event Action? RevertPressed;

    public event Action? ReloadPressed;

    public event Action<bool>? LiveToggled;

    /// <summary>The problems chip was clicked.</summary>
    public event Action? ProblemsPressed;

    public override void _Ready()
    {
        MapLook look = MapLook.Of(this);
        var panel = new StyleBoxFlat { BgColor = MapPalette.Panel, BorderColor = MapPalette.Line, BorderWidthBottom = 1 };
        panel.ContentMarginLeft = panel.ContentMarginRight = look.Px(18f);
        panel.ContentMarginTop = panel.ContentMarginBottom = look.Px(10f);
        AddThemeStyleboxOverride("panel", panel);
        _row.CustomMinimumSize = new Vector2(0f, look.Px(34f));
    }

    public void ShowLive(bool live) => _live.SetPressedNoSignal(live);

    /// <summary>Fills the page switch; the first page starts picked.</summary>
    public void ShowPages(string[] names, string[] tooltips)
    {
        for (int i = 0; i < names.Length; i++)
        {
            int index = i;
            var button = new Button
            {
                Text = names[i],
                TooltipText = i < tooltips.Length ? tooltips[i] : string.Empty,
                ThemeTypeVariation = MapTheme.Segment,
                ToggleMode = true,
                ButtonGroup = _pageGroup,
                ButtonPressed = i == 0,
                FocusMode = FocusModeEnum.None,
            };
            button.Pressed += () => PageChosen?.Invoke(index);
            _pages.AddChild(button);
        }
    }

    /// <param name="unsaved">Whether there is anything to save or revert.</param>
    /// <param name="tooltip">The details, e.g. which files are unsaved.</param>
    public void ShowStatus(string status, string tooltip, bool unsaved)
    {
        _status.Text = status;
        _status.TooltipText = tooltip;
        _save.Disabled = !unsaved;
        _save.Marked = unsaved;
        _menu.GetPopup().SetItemDisabled(RevertId, !unsaved);
    }

    /// <summary>Shows what the last check found, always visible like the Character tab's chip.</summary>
    /// <param name="count">Problems the last check or save found.</param>
    /// <param name="unsaved">Whether there are unsaved changes.</param>
    /// <param name="checkedChanges">Whether the unsaved changes were checked yet.</param>
    public void ShowProblems(int count, bool unsaved, bool checkedChanges)
    {
        (string text, Color color) = count > 0 ? ($"⚠ {count} problem{(count == 1 ? "" : "s")} · show", MapPalette.Bad)
            : !unsaved ? ("✓ saved, no problems", MapPalette.Good)
            : checkedChanges ? ("✓ changes pass the checks", MapPalette.Good)
            : ("checking…", MapPalette.Muted);
        _chip.Text = text;
        foreach (string name in new[] { "font_color", "font_hover_color", "font_pressed_color" })
        {
            _chip.AddThemeColorOverride(name, color);
        }
    }

    /// <summary>For a project without a data folder: nothing can be edited.</summary>
    public void Disable(string reason)
    {
        _menu.Disabled = true;
        _live.Disabled = true;
        _save.Disabled = true;
        _chip.Visible = false;
        _pages.Visible = false;
        _status.Text = reason;
    }

    // The same framed segment strip as the Character tab's lens switch.
    private Control PageFrame()
    {
        var frame = new PanelContainer();
        var box = new StyleBoxFlat { BgColor = MapPalette.Background, BorderColor = MapPalette.Line };
        box.SetBorderWidthAll(1);
        box.SetCornerRadiusAll(10);
        box.SetContentMarginAll(3);
        frame.AddThemeStyleboxOverride("panel", box);
        _pages.AddThemeConstantOverride("separation", 2);
        frame.AddChild(_pages);
        return frame;
    }
}
#endif
