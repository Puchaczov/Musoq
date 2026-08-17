using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedCodeSamplesSnapshotTests
{
    private readonly ILoggerResolver _loggerResolver = new TestsLoggerResolver();

    public static IEnumerable<object[]> SampleData => GeneratedCodeSamplesCatalog.Samples
        .Select(static sample => new object[] { sample });

    [TestMethod]
    public void CompleteLocalSamples_WhenComparedToCatalog_ShouldNotContainExtraFiles()
    {
        Assert.IsTrue(
            Directory.Exists(GeneratedCodeSampleArtifacts.SamplesDirectory),
            $"Tracked generated samples directory is missing: {GeneratedCodeSampleArtifacts.SamplesDirectory}");

        var localFiles = Directory
            .EnumerateFiles(GeneratedCodeSampleArtifacts.SamplesDirectory, "*.cs")
            .Select(static path => Path.GetFileName(path)!)
            .OrderBy(static fileName => fileName, StringComparer.Ordinal)
            .ToArray();

        var catalogFiles = GeneratedCodeSamplesCatalog.Samples
            .Select(static sample => sample.FileName)
            .OrderBy(static fileName => fileName, StringComparer.Ordinal)
            .ToArray();

        var missingLocalFiles = catalogFiles
            .Except(localFiles, StringComparer.Ordinal)
            .ToArray();
        Assert.IsEmpty(
            missingLocalFiles,
            $"Tracked generated samples are missing: {string.Join(", ", missingLocalFiles)}");

        var extraLocalFiles = localFiles
            .Except(catalogFiles, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            extraLocalFiles,
            $"Local generated samples are stale: {string.Join(", ", extraLocalFiles)}");
    }

    [TestMethod]
    public void GeneratedCorpus_WhenScanned_ShouldNotContainRetiredRuntimeDiagnosticContracts()
    {
        string[] forbiddenPatterns =
        [
            "RuntimeExpressionBoundary",
            "RuntimeExpressionOrigin",
            "RuntimeExpressionException",
            "WrapRuntimeExpressionBoundary"
        ];

        var offenders = new List<string>();
        foreach (var directory in new[]
                 {
                     GeneratedCodeSampleArtifacts.SamplesDirectory,
                     GeneratedCodeSampleArtifacts.ProfiledSamplesDirectory
                 })
        {
            if (!Directory.Exists(directory))
                continue;

            foreach (var path in Directory.EnumerateFiles(directory, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var content = File.ReadAllText(path);
                foreach (var pattern in forbiddenPatterns)
                {
                    if (content.Contains(pattern, StringComparison.Ordinal))
                        offenders.Add($"{Path.GetFileName(path)}: {pattern}");
                }
            }
        }

        Assert.IsEmpty(
            offenders,
            $"Generated code contains retired runtime diagnostic contracts: {string.Join(", ", offenders)}");
    }

    [TestMethod]
    public void Catalog_WhenLoaded_ShouldNotContainDuplicateFiles()
    {
        var duplicateFiles = GeneratedCodeSamplesCatalog.Samples
            .GroupBy(static sample => sample.FileName)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();

        Assert.IsEmpty(duplicateFiles, $"Duplicate generated sample files: {string.Join(", ", duplicateFiles)}");
    }

    [TestMethod]
    public void Catalog_WhenLoaded_ShouldNotContainBlankQueries()
    {
        var blankQueries = GeneratedCodeSamplesCatalog.Samples
            .Where(static sample => string.IsNullOrWhiteSpace(sample.Query))
            .Select(static sample => sample.FileName)
            .ToArray();

        Assert.IsEmpty(blankQueries, $"Generated samples with blank queries: {string.Join(", ", blankQueries)}");
    }

    [TestMethod]
    public void Catalog_WhenGenerated_ShouldExposeOrderedInspectionAndCodeSections()
    {
        var sample = GeneratedCodeSamplesCatalog.GetByFileName("Q223_RecursiveUncorrelatedApplySnapshot.cs");
        var generated = GeneratedCodeSampleArtifacts.Generate(sample, _loggerResolver);
        string[] markers =
        [
            "// === Parsed Query ===",
            "// === Logical Plan ===",
            "// === Physical Plan ===",
            "// === Execution Plan ===",
            "// === Generated C# ==="
        ];
        var previous = -1;
        foreach (var marker in markers)
        {
            var current = generated.IndexOf(marker, StringComparison.Ordinal);
            Assert.IsGreaterThan(previous, current, marker);
            previous = current;
        }
    }

    [TestMethod]
    [DynamicData(nameof(SampleData))]
    public void LocalSample_WhenRegenerated_ShouldMatchCatalogOutput(GeneratedCodeSample sample)
    {
        var samplePath = GeneratedCodeSampleArtifacts.GetSamplePath(sample);
        Assert.IsTrue(
            File.Exists(samplePath),
            $"Tracked generated sample is missing: {samplePath}");

        var expected = File.ReadAllText(samplePath);
        var actual = GeneratedCodeSampleArtifacts.Generate(sample, _loggerResolver);

        Assert.AreEqual(
            GeneratedCodeSampleArtifacts.NormalizeForComparison(expected),
            GeneratedCodeSampleArtifacts.NormalizeForComparison(actual),
            $"Generated sample {sample.FileName} is stale. Run {nameof(Refresh_All_Local_Generated_Samples)} intentionally to refresh snapshots.");
    }

    [TestMethod]
    [Ignore("Local snapshot refresh utility. Run intentionally when generated-code changes are expected.")]
    public void Refresh_All_Local_Generated_Samples()
    {
        foreach (var sample in GeneratedCodeSamplesCatalog.Samples)
            GeneratedCodeSampleArtifacts.Write(sample, _loggerResolver);
    }
}
