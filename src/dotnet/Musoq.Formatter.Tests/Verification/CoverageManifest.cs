using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Musoq.Formatter.Tests.Verification;

internal static class CoverageManifest
{
    private static readonly Lazy<JsonElement> Root = new(Load);
    internal static JsonElement[] Entries(string name) => Root.Value.GetProperty(name).EnumerateArray().ToArray();
    internal static string[] SourceTypes => Entries("parserNodes")
        .Where(entry => entry.GetProperty("disposition").GetString() == "source-syntax")
        .Select(entry => entry.GetProperty("fullName").GetString()!).ToArray();
    internal static string[] Inventory => Entries("parserNodes").Select(entry => entry.GetProperty("fullName").GetString()!).ToArray();

    private static JsonElement Load()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "SyntaxCoverage.json")));
        return document.RootElement.Clone();
    }
}
