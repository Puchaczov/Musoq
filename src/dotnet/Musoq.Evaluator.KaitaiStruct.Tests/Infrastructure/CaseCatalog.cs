using System.Text.Json;

namespace Musoq.Evaluator.KaitaiStruct.Tests.Infrastructure;

internal static class CaseCatalog
{
    public static string Root => AppContext.BaseDirectory;
    public static string ProjectPath(string path) => Path.Combine(Root, path.Replace('/', Path.DirectorySeparatorChar));

    public static IEnumerable<string> Ids => Directory.EnumerateDirectories(ProjectPath("Cases"))
        .Where(path => File.Exists(Path.Combine(path, "case.json")))
        .Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal);

    public static JsonDocument Load(string id) =>
        JsonDocument.Parse(File.ReadAllText(ProjectPath($"Cases/{id}/case.json")));

    internal static IEnumerable<object[]> Queries
    {
        get
        {
            foreach (var id in Ids)
            {
                using var document = Load(id);
                foreach (var query in document.RootElement.GetProperty("queries").EnumerateArray())
                    yield return new object[] { id, query.GetProperty("name").GetString()! };
            }
        }
    }

    internal static JsonElement[] Assertions(JsonElement test, JsonElement query)
    {
        if (query.TryGetProperty("assertions", out var supplemental))
            return supplemental.EnumerateArray().ToArray();
        var assertions = test.GetProperty("assertions").EnumerateArray().ToArray();
        return query.GetProperty("assertionIndices").EnumerateArray()
            .Select(index => assertions[index.GetInt32()]).ToArray();
    }
}
