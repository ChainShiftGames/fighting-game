namespace FightingGame.UI.Framework;

/// <summary>Turns a translation key into the text for the player's language.</summary>
public interface IMenuTranslator
{
    /// <summary>The translation; the key itself when it has none.</summary>
    string Translate(string key);
}
