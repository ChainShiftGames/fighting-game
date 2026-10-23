using System.Collections.Generic;
using FightingGame.UI.CharacterSelect;
using FightingGame.UI.Framework;

namespace FightingGame.Tests.UI.CharacterSelect;

/// <summary>Characters for the character select tests, each with as many colours as asked.</summary>
internal static class TestCharacters
{
    public static SelectableCharacter Named(string id, int colours = 3)
    {
        var swatches = new string[colours];
        for (int i = 0; i < colours; i++)
        {
            swatches[i] = "#ffffff";
        }

        return new SelectableCharacter(id, id, MenuText.Empty, swatches);
    }

    public static IReadOnlyList<SelectableCharacter> Three() => [Named("ryu"), Named("ken"), Named("guile")];
}
