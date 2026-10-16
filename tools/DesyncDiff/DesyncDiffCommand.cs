using System.IO;
using Netcode.Desync;

namespace DesyncDiff;

/// <summary>The command line: <c>DesyncDiff &lt;dump A&gt; &lt;dump B&gt;</c>.</summary>
public static class DesyncDiffCommand
{
    public const int Compared = 0;
    public const int Failed = 1;

    /// <returns><see cref="Compared"/> once the dumps are compared, <see cref="Failed"/> on bad arguments or files.</returns>
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length != 2)
        {
            error.WriteLine("Usage: DesyncDiff <dump A> <dump B>  (the two .json dumps of one desync, one from each PC)");
            return Failed;
        }

        DesyncDump first;
        DesyncDump second;
        try
        {
            first = DesyncDumpFile.Read(args[0]);
            second = DesyncDumpFile.Read(args[1]);
        }
        catch (IOException exception)
        {
            error.WriteLine(exception.Message);
            return Failed;
        }

        DiffReport.Write(output, first, second, DumpComparison.Compare(first, second));
        return Compared;
    }
}
