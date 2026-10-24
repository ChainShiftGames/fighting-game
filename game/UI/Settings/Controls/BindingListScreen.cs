using System.Collections.Generic;
using FightingGame.Input.Profiles;
using FightingGame.UI.Framework;

namespace FightingGame.UI.Settings.Controls;

/// <summary>
/// A few inputs of one layer (directions, buttons or menu buttons) with the controls bound to each; controls that
/// other inputs use too are pointed out above the list. Choosing one opens <see cref="InputBindingScreen"/>.
/// </summary>
/// <remarks>Kept short, one line per input: menus don't scroll, so each list must fit a 720p screen.</remarks>
public sealed class BindingListScreen : IMenuScreen
{
    private readonly ControlsMenu _menu;
    private readonly BindingLayer _layer;
    private readonly IReadOnlyList<string> _inputs;
    private readonly string _titleKey;

    public BindingListScreen(ControlsMenu menu, BindingLayer layer, IReadOnlyList<string> inputs, string titleKey)
    {
        _menu = menu;
        _layer = layer;
        _inputs = inputs;
        _titleKey = titleKey;
    }

    public MenuText Title => MenuText.Key(_titleKey);

    public IReadOnlyList<MenuText> Body
    {
        get
        {
            var lines = new List<MenuText> { MenuText.Key("MENU_CONTROLS_DEVICE_TITLE", _menu.Device.Name) };
            lines.AddRange(_menu.TestLines());
            foreach (string input in _inputs)
            {
                if (InputNames.Shared(_menu.Profile, _layer, input) is { IsEmpty: false } shared)
                {
                    lines.Add(shared);
                }
            }

            return lines;
        }
    }

    public IReadOnlyList<MenuItem> Items
    {
        get
        {
            BindingProfile profile = _menu.Profile;
            var items = new List<MenuItem>();
            foreach (string input in _inputs)
            {
                MenuText controls = InputNames.List(profile.ControlsOf(_layer, input));
                items.Add(new ButtonItem(
                    MenuText.Key("MENU_CONTROLS_BINDING", InputNames.Of(input), controls),
                    () => _menu.Push(new InputBindingScreen(_menu, _layer, input))));
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
