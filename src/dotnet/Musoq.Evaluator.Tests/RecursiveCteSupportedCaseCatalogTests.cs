using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class RecursiveCteSupportedCaseCatalogTests
{
    [TestMethod]
    public void Catalog_ShouldHaveStableUniqueCasesAndWellFormedExpectedRows()
    {
        var cases = RecursiveCteSupportedCaseCatalog.Cases;

        Assert.HasCount(68, cases);
        Assert.HasCount(cases.Count, cases.Select(static item => item.Name).Distinct(StringComparer.Ordinal));
        Assert.IsTrue(cases.All(static item => item.FactorTags.Count > 0));
        Assert.IsTrue(cases.All(static item => !string.IsNullOrWhiteSpace(item.Query)));
        Assert.IsTrue(cases.All(static item => item.ExpectedColumns.Count > 0));
        Assert.IsTrue(cases.All(static item => item.ExpectedRows.All(row =>
            row.Length == item.ExpectedColumns.Count)));
        Assert.IsTrue(cases.All(static item => item.ExpectedColumns
            .Select(static column => column.Name)
            .Distinct(StringComparer.Ordinal)
            .Count() == item.ExpectedColumns.Count));
    }

    [TestMethod]
    public void MixedCoverage_ShouldCoverEveryCompatibleDeclaredFactorPair()
    {
        var cases = RecursiveCteSupportedCaseCatalog.Cases;

        Assert.HasCount(8, cases.Where(static item =>
            item.FactorTags.Contains("targeted-three-way", StringComparer.Ordinal)));
        Assert.HasCount(18, cases.Where(static item =>
            item.Name.StartsWith("pair-", StringComparison.Ordinal)));

        var dimensions = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["identity"] = ["union-all", "union", "union-keyed"],
            ["topology"] = ["chain", "cycle"],
            ["source"] = ["values", "external-snapshot"]
        };
        var exclusions = new HashSet<string>(StringComparer.Ordinal)
        {
            PairKey("union-all", "cycle")
        };
        var requiredPairs = dimensions
            .SelectMany((left, leftIndex) => dimensions.Skip(leftIndex + 1)
                .SelectMany(right => left.Value.SelectMany(leftValue => right.Value.Select(rightValue =>
                    (Left: leftValue, Right: rightValue)))))
            .Where(pair => !exclusions.Contains(PairKey(pair.Left, pair.Right)))
            .ToArray();
        var missingPairs = requiredPairs
            .Where(pair => !cases.Any(testCase =>
                testCase.FactorTags.Contains(pair.Left) && testCase.FactorTags.Contains(pair.Right)))
            .Select(pair => PairKey(pair.Left, pair.Right))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(missingPairs, $"Missing compatible factor pairs: {string.Join(", ", missingPairs)}");
        Assert.HasCount(1, exclusions);
    }

    [TestMethod]
    public void SampleBackedCases_ShouldMapOneToOneToTrackedRecursiveSamples()
    {
        var caseSamples = RecursiveCteSupportedCaseCatalog.Cases
            .Where(static item => item.GeneratedSampleName != null)
            .Select(static item => $"{item.GeneratedSampleName}.cs")
            .Order(StringComparer.Ordinal)
            .ToArray();
        var catalogSamples = GeneratedCodeSamplesCatalog.Samples
            .Where(static item => item.Category == "RecursiveCte" &&
                                  item.Name != "Q187_CteColumnListOrdinary")
            .Select(static item => item.FileName)
            .Order(StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEqual(caseSamples, catalogSamples);
        Assert.HasCount(39, caseSamples);
        Assert.HasCount(40, GeneratedCodeSamplesCatalog.Samples
            .Where(static item => item.Category == "RecursiveCte"));
    }

    private static string PairKey(string left, string right) =>
        string.CompareOrdinal(left, right) <= 0 ? $"{left}|{right}" : $"{right}|{left}";
}
