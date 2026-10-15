namespace Sim.Analyzers.Tests;

/// <summary>Wraps test code in a compilable Sim source file.</summary>
internal static class Snippet
{
    public static string InMethod(string statements) => $$"""
        namespace Sim;

        public static class Probe
        {
            public static int Run()
            {
                int result = 0;
                {{statements}}
                return result;
            }
        }
        """;

    public static string InClass(string members) => $$"""
        namespace Sim;

        public class Probe
        {
            {{members}}
        }
        """;
}
