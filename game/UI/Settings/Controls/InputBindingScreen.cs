using System.Collections.Generic;
using FightingGame.Input.Profiles;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings.Controls;

/// <summary>One input's controls: change them for one new control, add another, or take one off.</summary>
public sealed class InputBindingScreen : IMenuScreen
{
    private readonly ControlsMenu _menu;
    private readonly BindingLayer _layer;
    private readonly string _input;

    public InputBindingScreen(ControlsMenu menu, BindingLayer layer, string input)
    {
        _menu = menu;
        _layer = layer;
        _input = input;
    }

    public MenuText Title => InputNames.Of(_input);

    public IReadOnlyList<MenuText> Body =>
    [
        MenuText.Key("MENU_CONTROLS_BOUND_NOW", InputNames.List(_menu.Profile.ControlsOf(_layer, _input))),
        InputNames.Shared(_menu.Profile, _layer, _input),
        .. _menu.TestLines(),
    ];

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            var items = new List<MenuItem>
            {
                new ButtonItem(MenuText.Key("MENU_CONTROLS_CHANGE"), () => _menu.StartCapture(_layer, _input, replace: true))
                {
                    Description = MenuText.Key("MENU_CONTROLS_CHANGE_INFO"),
                },
                new ButtonItem(MenuText.Key("MENU_CONTROLS_ADD"), () => _menu.StartCapture(_layer, _input, replace: false))
                {
                    Description = MenuText.Key("MENU_CONTROLS_ADD_INFO"),
                },
            };
            foreach (BoundControl control in _menu.Profile.ControlsOf(_layer, _input))
            {
                items.Add(new ButtonItem(
                    MenuText.Key("MENU_CONTROLS_REMOVE", InputNames.Of(control)),
                    () => _menu.Change(_menu.Profile.Unbind(_layer, _input, control))));
            }

            return items;
        }
    }

    public bool IsOverMatch => _menu.IsOverMatch;

    public bool Back()
    {
        _menu.Close(this);
        return false;
    }

    public void Press(MenuPress press)
    {
    }
}
