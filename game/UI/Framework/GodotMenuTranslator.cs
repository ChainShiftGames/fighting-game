using Godot;

namespace FightingGame.UI.Framework;

/// <summary>Translates through Godot's translation server (the CSV table registered in project.godot).</summary>
public sealed class GodotMenuTranslator : IMenuTranslator
{
    public string Translate(string key) => TranslationServer.Translate(key).ToString();
}
