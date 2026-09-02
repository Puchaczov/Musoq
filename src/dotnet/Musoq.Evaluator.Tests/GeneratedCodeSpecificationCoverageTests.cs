using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Architecture;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedCodeSpecificationCoverageTests
{
    private static readonly string[] PermittedSpecifications =
    [
        "specs/musoq-core-language-spec.md",
        "specs/musoq-table-couple-spec.md",
        "specs/musoq-binary-text-spec.md"
    ];

    private static readonly HashSet<string> PermittedSpecificationSet =
        new(PermittedSpecifications, StringComparer.Ordinal);

    private static readonly HashSet<string> PermittedDispositions =
        new(["sampled", "equivalent-lowering", "excluded"], StringComparer.Ordinal);

    private static readonly HashSet<string> PermittedExclusionKinds =
        new(["non-codegen", "diagnostic-only", "host-only", "unsupported", "future"], StringComparer.Ordinal);

    private static readonly Regex HeadingPattern = new(@"^#{1,6}\s+", RegexOptions.CultureInvariant);
    private static readonly Regex SampleFilePattern = new(@"^Q(\d+)_.*\.cs$", RegexOptions.CultureInvariant);
    private static readonly Regex FeatureIdPattern = new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant);

    [TestMethod]
    public void Ledger_ContainsOnlyPermittedNonAiSpecifications()
    {
        using var ledger = LoadLedger();
        var specifications = ledger.RootElement.GetProperty("specifications")
            .EnumerateArray()
            .Select(static value => value.GetString() ?? string.Empty)
            .ToArray();

        CollectionAssert.AreEquivalent(PermittedSpecifications, specifications);
        Assert.IsFalse(
            File.ReadAllText(CoveragePath()).Contains("musoq-ai-spec", StringComparison.OrdinalIgnoreCase),
            "The AI specification must be absent from the generated-code coverage contract.");
    }

    [TestMethod]
    public void Ledger_ClassifiesEveryCurrentSpecificationHeading()
    {
        using var ledger = LoadLedger();
        var classifiedBySpecification = ledger.RootElement
            .GetProperty("features")
            .EnumerateArray()
            .GroupBy(static feature => feature.GetProperty("specification").GetString()!, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.SelectMany(static feature => feature.GetProperty("sections").EnumerateArray())
                    .Select(static section => section.GetString()!)
                    .ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);

        foreach (var specification in PermittedSpecifications)
        {
            var missing = ReadHeadings(specification)
                .Where(heading => !classifiedBySpecification.TryGetValue(specification, out var sections) || !sections.Contains(heading))
                .ToArray();

            Assert.IsEmpty(missing, $"Unclassified headings in {specification}: {string.Join("; ", missing)}");
        }
    }

    [TestMethod]
    public void Ledger_FeatureIdsAndDispositionFieldsAreValid()
    {
        using var ledger = LoadLedger();
        var features = ledger.RootElement.GetProperty("features").EnumerateArray().ToArray();
        var ids = features.Select(static feature => feature.GetProperty("id").GetString()!).ToArray();

        Assert.IsNotEmpty(features);
        Assert.HasCount(ids.Length, ids.Distinct(StringComparer.Ordinal));

        foreach (var feature in features)
        {
            var id = feature.GetProperty("id").GetString() ?? string.Empty;
            var specification = feature.GetProperty("specification").GetString() ?? string.Empty;
            var disposition = feature.GetProperty("disposition").GetString() ?? string.Empty;
            var sections = feature.GetProperty("sections").EnumerateArray().ToArray();
            var sampleFiles = feature.GetProperty("sampleFiles").EnumerateArray().ToArray();
            var evidence = feature.GetProperty("evidence").GetString();
            var reason = feature.GetProperty("reason").GetString();
            var exclusionKind = feature.GetProperty("exclusionKind");

            Assert.IsTrue(FeatureIdPattern.IsMatch(id), $"Feature id is not stable kebab-case: {id}");
            Assert.IsTrue(PermittedSpecificationSet.Contains(specification), $"Unexpected specification: {specification}");
            Assert.IsTrue(sections.Length > 0, $"Feature has no specification headings: {id}");
            Assert.IsTrue(PermittedDispositions.Contains(disposition), $"Unexpected disposition for {id}: {disposition}");
            Assert.IsFalse(string.IsNullOrWhiteSpace(evidence), $"Feature has no evidence: {id}");

            if (disposition is "sampled" or "equivalent-lowering")
            {
                Assert.IsTrue(sampleFiles.Length > 0, $"Sampled feature has no files: {id}");
                Assert.IsTrue(string.IsNullOrWhiteSpace(exclusionKind.GetString()), $"Sampled feature has exclusion kind: {id}");
                if (disposition == "sampled")
                    Assert.IsTrue(string.IsNullOrWhiteSpace(reason), $"Sampled feature should not carry a mismatch reason: {id}");
                else
                    Assert.IsFalse(string.IsNullOrWhiteSpace(reason), $"Equivalent feature needs an explanation: {id}");
            }
            else
            {
                Assert.AreEqual(JsonValueKind.Array, feature.GetProperty("sampleFiles").ValueKind);
                Assert.IsEmpty(sampleFiles, $"Excluded feature references generated files: {id}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(reason), $"Excluded feature needs an explanation: {id}");
                Assert.IsTrue(exclusionKind.ValueKind == JsonValueKind.String &&
                              PermittedExclusionKinds.Contains(exclusionKind.GetString()!),
                    $"Excluded feature has invalid exclusion kind: {id}");
            }
        }
    }

    [TestMethod]
    public void Ledger_SampledAndEquivalentFilesMatchCatalogDiskAndManifest()
    {
        using var ledger = LoadLedger();
        var catalogFiles = GeneratedCodeSamplesCatalog.Samples
            .Select(static sample => sample.FileName)
            .ToHashSet(StringComparer.Ordinal);
        var manifestFiles = ReadManifestFiles();
        var currentDirectory = Path.Combine(RepositorySourceScan.RepositoryRoot(), "generated-code-samples", "current");

        foreach (var feature in ledger.RootElement.GetProperty("features").EnumerateArray()
                     .Where(static feature => feature.GetProperty("disposition").GetString() is "sampled" or "equivalent-lowering"))
        {
            var id = feature.GetProperty("id").GetString()!;
            foreach (var file in feature.GetProperty("sampleFiles").EnumerateArray().Select(static value => value.GetString()!))
            {
                Assert.IsTrue(catalogFiles.Contains(file), $"{id} references a file absent from the catalog: {file}");
                Assert.IsTrue(File.Exists(Path.Combine(currentDirectory, file)), $"{id} references a missing snapshot: {file}");
                Assert.IsTrue(manifestFiles.Contains(file), $"{id} references a file absent from the manifest: {file}");
            }
        }
    }

    [TestMethod]
    public void Ledger_ReferencesEveryNewGapSnapshot()
    {
        using var ledger = LoadLedger();
        var references = ledger.RootElement.GetProperty("features")
            .EnumerateArray()
            .SelectMany(static feature => feature.GetProperty("sampleFiles").EnumerateArray())
            .Select(static value => value.GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var newFiles = GeneratedCodeSamplesCatalog.Samples
            .Select(static sample => sample.FileName)
            .Where(static file => IsNewGapFile(file))
            .ToArray();

        Assert.HasCount(56, newFiles);
        var missing = newFiles.Where(file => !references.Contains(file)).ToArray();
        Assert.IsEmpty(missing, $"New Q268-Q323 snapshots are not referenced by the ledger: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void Ledger_RecordsExplicitBoundaryExclusions()
    {
        using var ledger = LoadLedger();
        var byId = ledger.RootElement.GetProperty("features")
            .EnumerateArray()
            .ToDictionary(static feature => feature.GetProperty("id").GetString()!, StringComparer.Ordinal);

        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["spec-core-host-system-range-boundary"] = "host-only",
            ["spec-core-asof-right-join-boundary"] = "unsupported",
            ["spec-core-window-groups-exclude-boundary"] = "unsupported",
            ["spec-core-positional-order-by-boundary"] = "unsupported",
            ["spec-core-recursive-cte-boundary"] = "unsupported",
            ["spec-core-advisory-and-diagnostics"] = "diagnostic-only",
            ["spec-table-diagnostics"] = "diagnostic-only",
            ["spec-binary-interpretation-outside-apply"] = "unsupported",
            ["spec-binary-switch-substream-boundary"] = "unsupported",
            ["spec-binary-diagnostics"] = "diagnostic-only",
            ["spec-binary-future-extensions"] = "future"
        };

        foreach (var (id, kind) in expected)
        {
            Assert.IsTrue(byId.TryGetValue(id, out var feature), $"Missing explicit boundary feature: {id}");
            Assert.AreEqual("excluded", feature.GetProperty("disposition").GetString(), id);
            Assert.AreEqual(kind, feature.GetProperty("exclusionKind").GetString(), id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(feature.GetProperty("reason").GetString()), id);
        }
    }

    private static bool IsNewGapFile(string fileName)
    {
        var match = SampleFilePattern.Match(fileName);
        return match.Success && int.TryParse(match.Groups[1].Value, out var number) && number is >= 268 and <= 323;
    }

    private static IEnumerable<string> ReadHeadings(string specification)
    {
        var path = Path.Combine(RepositorySourceScan.RepositoryRoot(), specification.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadLines(path)
            .Where(static line => HeadingPattern.IsMatch(line))
            .Select(static line => HeadingPattern.Replace(line, string.Empty).Trim());
    }

    private static HashSet<string> ReadManifestFiles()
    {
        var path = Path.Combine(RepositorySourceScan.RepositoryRoot(), "src", "dotnet", "Musoq.Evaluator.Tests", "GeneratedCodeSamplesManifest.txt");
        return File.ReadLines(path)
            .Where(static line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#'))
            .Select(static line => line.Split('\t', 2)[0])
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string CoveragePath() => Path.Combine(RepositorySourceScan.RepositoryRoot(), "specs", "generated-code-sample-coverage.json");

    private static JsonDocument LoadLedger() => JsonDocument.Parse(File.ReadAllText(CoveragePath()));
}
