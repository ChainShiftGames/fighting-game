using System.Text.RegularExpressions;

namespace Architecture.Tests;

/// <summary>Regex patterns for the ArchUnitNET selectors these tests share.</summary>
internal static class TypeSelection
{
    // ArchUnitNET matches against the full assembly name ("Sim, Version=..., Culture=..."), so stop at the comma.
    public static string AssemblyPattern(string assemblyName) => $"^{Regex.Escape(assemblyName)}(,|$)";
}
