using System;
using System.IO;

namespace Netcode.Tests.Desync;

/// <summary>An empty folder of its own for one test, deleted afterwards.</summary>
internal sealed class TemporaryFolder : IDisposable
{
    public string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fg-desync-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
