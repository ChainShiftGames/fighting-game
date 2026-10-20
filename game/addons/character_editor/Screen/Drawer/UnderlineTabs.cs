#if TOOLS
using System;
using FightingGame.Addons.CharacterEditor.Screen.Look;
using Godot;

namespace FightingGame.Addons.CharacterEditor.Screen.Drawer;

/// <summary>The drawer's page tabs: plain words on a hairline, the open one lit and underlined.</summary>
[Tool]
public partial class UnderlineTabs : HBoxContainer
{
    private readonly ButtonGroup _group = new();

    public UnderlineTabs()
    {
        AddThemeConstantOverride("separation", 16);
    }

    /// <summary>The user opened the tab at this index.</summary>
    public event Action<int>? TabChosen;

    public int Current { get; private set; }

    public void SetTabs(params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            int index = i;
            var tab = new Button
            {
                Text = names[i],
                ThemeTypeVariation = MapTheme.Tab,
                ToggleMode = true,
                ButtonGroup = _group,
                ButtonPressed = i == 0,
                FocusMode = FocusModeEnum.None,
                MouseDefaultCursorShape = CursorShape.PointingHand,
            };
            tab.Pressed += () => Open(index);
            AddChild(tab);
        }
    }

    /// <summary>Opens the tab as if clicked.</summary>
    public void Open(int index)
    {
        Current = index;
        if (GetChild(index) is Button tab)
        {
            tab.SetPressedNoSignal(true);
        }

        QueueRedraw();
        TabChosen?.Invoke(index);
    }

    public override void _Draw()
    {
        MapLook look = MapLook.Of(this);
        float line = Mathf.Max(1f, look.Px(1f));
        DrawRect(new Rect2(0f, Size.Y - line, Size.X, line), MapPalette.Line);
        if (Current < GetChildCount() && GetChild(Current) is Control tab)
        {
            float thick = look.Px(2f);
            DrawRect(new Rect2(tab.Position.X, Size.Y - thick, tab.Size.X, thick), MapPalette.Glow);
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationSortChildren)
        {
            QueueRedraw();
        }
    }
}
#endif
