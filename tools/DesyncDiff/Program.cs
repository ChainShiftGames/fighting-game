using System;

namespace DesyncDiff;

/// <summary>Entry point; see <see cref="DesyncDiffCommand"/>.</summary>
public static class Program
{
    public static int Main(string[] args) => DesyncDiffCommand.Run(args, Console.Out, Console.Error);
}
