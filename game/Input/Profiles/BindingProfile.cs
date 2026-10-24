using System;
using System.Collections.Generic;

namespace FightingGame.Input.Profiles;

/// <summary>
/// One device's layout (player settings, ARCHITECTURE.md §6): the controls bound to each game and menu input, and a
/// joypad's stick options. Immutable: every edit gives a new profile. Several controls per input are fine, and so is
/// one control on several inputs (a key for special + C at once); the menu warns about the second (UI-10).
/// </summary>
public sealed class BindingProfile
{
    private readonly ProfileBinding[] _game;
    private readonly ProfileBinding[] _menu;

    public BindingProfile(
        InputDeviceKind kind, IReadOnlyList<ProfileBinding> game, IReadOnlyList<ProfileBinding> menu, StickOptions stick)
    {
        Kind = kind;
        _game = [.. game];
        _menu = [.. menu];
        Stick = kind == InputDeviceKind.Joypad ? stick : StickOptions.None;
    }

    public InputDeviceKind Kind { get; }

    public IReadOnlyList<ProfileBinding> Game => _game;

    public IReadOnlyList<ProfileBinding> Menu => _menu;

    public StickOptions Stick { get; }

    public IReadOnlyList<ProfileBinding> Layer(BindingLayer layer) => layer == BindingLayer.Game ? _game : _menu;

    /// <summary>The controls bound to <paramref name="input"/>, in the order they were bound.</summary>
    public List<BoundControl> ControlsOf(BindingLayer layer, string input)
    {
        var controls = new List<BoundControl>();
        foreach (ProfileBinding binding in Layer(layer))
        {
            if (binding.Input == input)
            {
                controls.Add(binding.Control);
            }
        }

        return controls;
    }

    /// <summary>The inputs of <paramref name="layer"/> other than <paramref name="except"/> that use <paramref name="control"/>.</summary>
    public List<string> InputsBoundTo(BindingLayer layer, BoundControl control, string? except = null)
    {
        var inputs = new List<string>();
        foreach (ProfileBinding binding in Layer(layer))
        {
            if (binding.Control == control && binding.Input != except && !inputs.Contains(binding.Input))
            {
                inputs.Add(binding.Input);
            }
        }

        return inputs;
    }

    /// <summary>Binds <paramref name="control"/> to <paramref name="input"/>; <paramref name="replace"/> first unbinds the input's other controls.</summary>
    public BindingProfile Bind(BindingLayer layer, string input, BoundControl control, bool replace)
    {
        var bindings = new List<ProfileBinding>();
        foreach (ProfileBinding binding in Layer(layer))
        {
            bool isReplaced = binding.Input == input && (replace || binding.Control == control);
            if (!isReplaced)
            {
                bindings.Add(binding);
            }
        }

        bindings.Add(new ProfileBinding(input, control));
        return WithLayer(layer, bindings);
    }

    public BindingProfile Unbind(BindingLayer layer, string input, BoundControl control) =>
        WithLayer(layer, Without(layer, binding => binding.Input == input && binding.Control == control));

    /// <summary>Takes <paramref name="control"/> off every input of <paramref name="layer"/> but <paramref name="keepOn"/>.</summary>
    public BindingProfile Release(BindingLayer layer, BoundControl control, string keepOn) =>
        WithLayer(layer, Without(layer, binding => binding.Control == control && binding.Input != keepOn));

    public BindingProfile WithStick(StickOptions stick) => new(Kind, _game, _menu, stick);

    /// <summary>Same kind, options and bindings, whatever order they were bound in.</summary>
    public bool SameAs(BindingProfile other) =>
        Kind == other.Kind && Stick == other.Stick && SameSet(_game, other._game) && SameSet(_menu, other._menu);

    private static bool SameSet(ProfileBinding[] first, ProfileBinding[] second)
    {
        var firstSet = new HashSet<ProfileBinding>(first);
        return firstSet.SetEquals(second);
    }

    private List<ProfileBinding> Without(BindingLayer layer, Predicate<ProfileBinding> isRemoved)
    {
        var bindings = new List<ProfileBinding>(Layer(layer));
        bindings.RemoveAll(isRemoved);
        return bindings;
    }

    private BindingProfile WithLayer(BindingLayer layer, List<ProfileBinding> bindings) =>
        layer == BindingLayer.Game ? new(Kind, bindings, _menu, Stick) : new(Kind, _game, bindings, Stick);
}
