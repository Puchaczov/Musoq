using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedCodeProfiledSamplesSnapshotTests
{
    private readonly ILoggerResolver _loggerResolver = new TestsLoggerResolver();

    public static IEnumerable<object[]> SampleData => GeneratedCodeProfiledSamplesCatalog.Samples
        .Select(static sample => new object[] { sample });

    [TestMethod]
    public void CompleteLocalProfiledSamples_WhenComparedToCatalog_ShouldNotContainExtraFiles()
    {
        Assert.IsTrue(
            Directory.Exists(GeneratedCodeSampleArtifacts.ProfiledSamplesDirectory),
            $"Tracked profiled generated samples directory is missing: {GeneratedCodeSampleArtifacts.ProfiledSamplesDirectory}");

        var localFiles = Directory
            .EnumerateFiles(GeneratedCodeSampleArtifacts.ProfiledSamplesDirectory, "*.cs")
            .Select(static path => Path.GetFileName(path)!)
            .OrderBy(static fileName => fileName, StringComparer.Ordinal)
            .ToArray();

        var catalogFiles = GeneratedCodeProfiledSamplesCatalog.Samples
            .Select(static sample => sample.FileName)
            .OrderBy(static fileName => fileName, StringComparer.Ordinal)
            .ToArray();

        var missingLocalFiles = catalogFiles
            .Except(localFiles, StringComparer.Ordinal)
            .ToArray();
        Assert.IsEmpty(
            missingLocalFiles,
            $"Tracked profiled generated samples are missing: {string.Join(", ", missingLocalFiles)}");

        var extraLocalFiles = localFiles
            .Except(catalogFiles, StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            extraLocalFiles,
            $"Local profiled generated samples are stale: {string.Join(", ", extraLocalFiles)}");
    }

    [TestMethod]
    public void Catalog_WhenLoaded_ShouldNotContainDuplicateProfiledFiles()
    {
        var duplicateFiles = GeneratedCodeProfiledSamplesCatalog.Samples
            .GroupBy(static sample => sample.FileName)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .ToArray();

        Assert.IsEmpty(duplicateFiles, $"Duplicate profiled generated sample files: {string.Join(", ", duplicateFiles)}");
    }

    [TestMethod]
    public void Catalog_WhenLoaded_ShouldNotContainBlankProfiledQueries()
    {
        var blankQueries = GeneratedCodeProfiledSamplesCatalog.Samples
            .Where(static sample => string.IsNullOrWhiteSpace(sample.Query))
            .Select(static sample => sample.FileName)
            .ToArray();

        Assert.IsEmpty(blankQueries, $"Profiled generated samples with blank queries: {string.Join(", ", blankQueries)}");
    }

    [TestMethod]
    [DynamicData(nameof(SampleData))]
    public void LocalProfiledSample_WhenRegenerated_ShouldMatchCatalogOutput(GeneratedCodeSample sample)
    {
        var samplePath = GeneratedCodeSampleArtifacts.GetProfiledSamplePath(sample);
        Assert.IsTrue(
            File.Exists(samplePath),
            $"Tracked profiled generated sample is missing: {samplePath}");

        var expected = File.ReadAllText(samplePath);
        var actual = GeneratedCodeSampleArtifacts.Generate(sample, _loggerResolver);

        Assert.AreEqual(
            GeneratedCodeSampleArtifacts.NormalizeForComparison(expected),
            GeneratedCodeSampleArtifacts.NormalizeForComparison(actual),
            $"Profiled generated sample {sample.FileName} is stale. Run {nameof(Refresh_All_Local_Profiled_Generated_Samples)} intentionally to refresh snapshots.");
    }

    [TestMethod]
    [Ignore("Local snapshot refresh utility. Run intentionally when profiled generated-code changes are expected.")]
    public void Refresh_All_Local_Profiled_Generated_Samples()
    {
        foreach (var sample in GeneratedCodeProfiledSamplesCatalog.Samples)
            GeneratedCodeSampleArtifacts.WriteProfiled(sample, _loggerResolver);
    }
}
