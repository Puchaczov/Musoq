using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Musoq.Formatter.Tests.Verification;

internal static class CorpusFixtures
{
    private static readonly Lazy<IReadOnlyList<Fixture>> Cases = new(() => Array.AsReadOnly(Read()));

    internal static IReadOnlyList<Fixture> Load() => Cases.Value;

    private static Fixture[] Read() => new[] { "SpecificationCorpus.json", "LanguageCoverage.json" }
        .SelectMany(name => JsonSerializer.Deserialize<Fixture[]>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name)),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!).ToArray();

    internal sealed record Fixture(string Id, string Source);
}
