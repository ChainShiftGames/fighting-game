using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Architecture.Tests;

/// <summary>Reads the assembly references the compiler emitted, without loading the assembly.</summary>
internal static class CompiledReferences
{
    // Compiled references are what ADR-0009 checks: an analyzer ProjectReference (ReferenceOutputAssembly=false)
    // never shows up here, and neither does a project reference the code doesn't use.
    public static IReadOnlyList<string> Of(string assemblyName)
    {
        using FileStream stream = File.OpenRead(ArchitectureAssemblies.PathOf(assemblyName));
        using var peReader = new PEReader(stream);
        MetadataReader metadata = peReader.GetMetadataReader();
        return metadata.AssemblyReferences
            .Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name))
            .ToList();
    }
}
