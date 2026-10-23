using System;
using System.Collections.Generic;

namespace FightingGame.UI.Framework;

/// <summary>
/// One piece of menu text: a translation key (from <c>Assets/UI/menu_text.csv</c>) with its arguments, or text shown
/// as it is, such as a player's name or an error. Menus build text from keys; <see cref="Plain"/> is only for words
/// the game didn't write.
/// </summary>
public sealed class MenuText
{
    private MenuText(string value, bool isKey, MenuText[] arguments)
    {
        Value = value;
        IsKey = isKey;
        Arguments = arguments;
    }

    public static MenuText Empty { get; } = new(string.Empty, isKey: false, []);

    /// <summary>The key, or the text itself when <see cref="IsKey"/> is false.</summary>
    public string Value { get; }

    public bool IsKey { get; }

    /// <summary>Fill the translation's <c>{0}</c>, <c>{1}</c>…, each translated in turn.</summary>
    public IReadOnlyList<MenuText> Arguments { get; }

    public bool IsEmpty => Value.Length == 0;

    public static MenuText Key(string key) => Key(key, Array.Empty<MenuText>());

    public static MenuText Key(string key, params MenuText[] arguments)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return new MenuText(key, isKey: true, arguments);
    }

    /// <summary>A key with untranslated arguments, e.g. <c>Key("LOBBY_INVITE_FRIEND", friend.Name)</c>.</summary>
    public static MenuText Key(string key, params string[] arguments)
    {
        var texts = new MenuText[arguments.Length];
        for (int i = 0; i < arguments.Length; i++)
        {
            texts[i] = Plain(arguments[i]);
        }

        return Key(key, texts);
    }

    public static MenuText Plain(string text) => text.Length == 0 ? Empty : new MenuText(text, isKey: false, []);

    public override string ToString() => IsKey ? $"[{Value}]" : Value;
}
