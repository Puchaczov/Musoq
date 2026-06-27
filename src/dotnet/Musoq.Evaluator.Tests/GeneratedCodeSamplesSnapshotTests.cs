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
        if (!Directory.Exists(GeneratedCodeSampleArtifacts.SamplesDirectory))
            return;

        var localFiles = Directory
            .EnumerateFiles(GeneratedCodeSampleArtifacts.SamplesDirectory, "*.cs")
            .Select(Path.GetFileName)
            .OrderBy(static fileName => fileName, StringComparer.Ordinal)
            .ToArray();
        if (localFiles.Length == 0)
            return;

        var catalogFiles = GeneratedCodeSamplesCatalog.Samples
            .Select(static sample => sample.FileName)
            .OrderBy(static fileName => fileName, StringComparer.Ordinal)
            .ToArray();

        var missingLocalFiles = catalogFiles
            .Except(localFiles, StringComparer.Ordinal)
            .ToArray();
        if (missingLocalFiles.Length > 0)
            return;

        var extraLocalFiles = localFiles
            .Except(catalogFiles, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            extraLocalFiles,
            $"Local generated samples are stale: {string.Join(", ", extraLocalFiles)}");
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
    [DynamicData(nameof(SampleData))]
    public void LocalSample_WhenRegenerated_ShouldMatchCatalogOutput(GeneratedCodeSample sample)
    {
        var samplePath = GeneratedCodeSampleArtifacts.GetSamplePath(sample);
        if (!File.Exists(samplePath))
            return;

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
