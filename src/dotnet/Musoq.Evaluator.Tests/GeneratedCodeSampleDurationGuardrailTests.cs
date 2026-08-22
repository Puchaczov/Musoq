using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Architecture;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedCodeSampleDurationGuardrailTests
{
    private const int ExpectedSampleCount = 250;

    private static readonly string[] CorpusWideAccessorFiles =
    [
        "GeneratedCodeSamplesShapeTests.BudgetsAndInterpretation.cs",
        "GeneratedCodeSamplesShapeTests.CorpusAndRuntime.cs",
        "GeneratedCodeSamplesShapeTests.FinalOutputAndPostOperations.cs",
        "GeneratedCodeSamplesShapeTests.OrderingAndAggregateLowering.cs",
        "GeneratedCodeSamplesShapeTests.OperatorBudgets.cs",
        "GeneratedCodeSamplesShapeTests.RowBufferSafeParallelCte.cs",
        "GeneratedCodeSamplesShapeTests.RuntimeHotPathClosure.cs",
        "GeneratedCodeSamplesShapeTests.RuntimeV2ProjectionAndCorpusGuards.cs",
        "GeneratedCodeSamplesShapeTests.SampleAccess.cs",
        "GeneratedCodeSamplesShapeTests.WindowsCtesOrderingAndAggregates.cs",
        "GeneratedCodeSamplesShapeTests.WindowsNoBoxing.cs"
    ];

    private static readonly string[] UncachedGenerationAllowList =
    [
        "GeneratedCodeSampleArtifacts.cs",
        "GeneratedCodeSamplesManifestTests.cs"
    ];

    [TestMethod]
    public void CurrentCorpus_ShouldKeepAllSnapshotsAndTrackedHashesCurrent()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var snapshotDirectory = GeneratedCodeSampleArtifacts.SamplesDirectory;
        var snapshotFiles = Directory
            .EnumerateFiles(snapshotDirectory, "*.cs")
            .Select(Path.GetFileName)
            .OrderBy(static fileName => fileName, StringComparer.Ordinal)
            .ToArray();

        Assert.AreEqual(ExpectedSampleCount, GeneratedCodeSamplesCatalog.Samples.Count);
        Assert.AreEqual(ExpectedSampleCount, snapshotFiles.Length);

        var manifestPath = Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator.Tests",
            "GeneratedCodeSamplesManifest.txt");
        var trackedManifest = File.ReadAllLines(manifestPath)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Where(static line => !line.StartsWith("#", StringComparison.Ordinal))
            .ToArray();
        var generatedManifest = GeneratedCodeSamplesManifestTests.CreateManifestLines();

        Assert.AreEqual(ExpectedSampleCount, trackedManifest.Length);
        CollectionAssert.AreEqual(trackedManifest, generatedManifest);
    }

    [TestMethod]
    public void PerSampleGeneration_ShouldUseTheSharedCacheOutsideRefreshUtilities()
    {
        var sourceFiles = GetEvaluatorTestSourceFiles();
        var artifactFile = sourceFiles.Single(static file =>
            Path.GetFileName(file).Equals("GeneratedCodeSampleArtifacts.cs", StringComparison.Ordinal));
        var artifactSource = File.ReadAllText(artifactFile);

        Assert.Contains("Cache.GetOrAdd(", artifactSource);
        Assert.Contains("GenerateCore" + "WithTiming", artifactSource);

        var directFactoryToken = "GenerateCore" + "WithTiming(";
        var directFactoryCalls = sourceFiles
            .Where(file => !Path.GetFileName(file).Equals("GeneratedCodeSampleArtifacts.cs", StringComparison.Ordinal))
            .Where(file => File.ReadAllText(file).Contains(directFactoryToken, StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToArray();

        Assert.IsEmpty(
            directFactoryCalls,
            $"Tests must call the cached Generate path, not the generation factory: {string.Join(", ", directFactoryCalls)}");
    }

    [TestMethod]
    public void UncachedGeneration_ShouldRemainRestrictedToRefreshUtilities()
    {
        var token = "Generate" + "UncachedForRefresh";
        var actual = GetEvaluatorTestSourceFiles()
            .Where(file => File.ReadAllText(file).Contains(token + "(", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .OrderBy(static fileName => fileName, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEquivalent(UncachedGenerationAllowList, actual);
    }

    [TestMethod]
    public void ReadAllSamples_ShouldRemainLimitedToCorpusWideTests()
    {
        var token = "Read" + "AllSamples";
        var pattern = new Regex(
            Regex.Escape(token) + @"\s*\(",
            RegexOptions.CultureInvariant);
        var actual = GetShapeTestSourceFiles()
            .Where(file => pattern.IsMatch(File.ReadAllText(file)))
            .Select(Path.GetFileName)
            .OrderBy(static fileName => fileName, StringComparer.Ordinal)
            .ToArray();

        CollectionAssert.AreEquivalent(CorpusWideAccessorFiles, actual);
    }

    private static string[] GetEvaluatorTestSourceFiles()
    {
        return Directory
            .EnumerateFiles(GetEvaluatorTestDirectory(), "*.cs", SearchOption.AllDirectories)
            .Where(static file =>
                !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
                !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .ToArray();
    }

    private static string[] GetShapeTestSourceFiles()
    {
        return Directory
            .EnumerateFiles(GetEvaluatorTestDirectory(), "GeneratedCodeSamplesShapeTests*.cs")
            .ToArray();
    }

    private static string GetEvaluatorTestDirectory()
    {
        return Path.Combine(
            RepositorySourceScan.RepositoryRoot(),
            "src",
            "dotnet",
            "Musoq.Evaluator.Tests");
    }
}
