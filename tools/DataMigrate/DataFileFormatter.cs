using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using Sim.Data.Json;

namespace DataMigrate;

/// <summary>Rewrites a JSON file in the shared hand-written layout of <c>data/</c>; its content stays the same.</summary>
public static class DataFileFormatter
{
    public static void Format(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        JsonNode root = JsonNode.Parse(bytes)!;
        File.WriteAllText(path, JsonDataWriter.Write(root, ListLineStarts.Read(bytes)), new UTF8Encoding(false));
    }
}
