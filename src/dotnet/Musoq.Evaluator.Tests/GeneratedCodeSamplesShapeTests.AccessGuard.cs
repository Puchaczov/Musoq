using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public sealed partial class GeneratedCodeSamplesShapeTests
{
    [TestMethod]
    public void TargetedSampleAccess_WhenRequested_ShouldNotUseTheCorpusAccessor()
    {
        var sample = ReadSample("Q01_SimpleSelectWhere.cs");

        Assert.AreEqual("Q01_SimpleSelectWhere.cs", sample.FileName);
        Assert.Contains("// === Generated C# ===", sample.Content);
    }

    [TestMethod]
    public void FullCorpusRepository_WhenMaterialized_ShouldPreserveCatalogOrder()
    {
        var actual = GeneratedCodeSampleCorpus.ReadAll();

        Assert.HasCount(GeneratedCodeSamplesCatalog.Samples.Count, actual);
        for (var index = 0; index < actual.Length; index++)
            Assert.AreEqual(GeneratedCodeSamplesCatalog.Samples[index].FileName, actual[index].FileName);
    }

    [TestMethod]
    public void ShapeTests_ShouldNotUseRemovedEagerSampleAccessorOrSingleFromCorpus()
    {
        var sourceDirectory = GetShapeTestSourceDirectory();
        var sourceFiles = Directory.EnumerateFiles(
                sourceDirectory,
                "GeneratedCodeSamplesShapeTests*.cs",
                SearchOption.TopDirectoryOnly)
            .ToArray();

        Assert.IsNotEmpty(sourceFiles);

        var eagerAccessorFiles = sourceFiles
            .Where(fileName => File.ReadAllText(fileName).Contains("Read" + "Samples()", StringComparison.Ordinal))
            .Select(Path.GetFileName)
            .ToArray();
        Assert.IsEmpty(eagerAccessorFiles, string.Join(Environment.NewLine, eagerAccessorFiles));

        var singleFromCorpusFiles = sourceFiles
            .Where(fileName => Regex.IsMatch(
                File.ReadAllText(fileName),
                @"ReadAllSamples\(\)\s*\.\s*Single\(",
                RegexOptions.CultureInvariant))
            .Select(Path.GetFileName)
            .ToArray();
        Assert.IsEmpty(singleFromCorpusFiles, string.Join(Environment.NewLine, singleFromCorpusFiles));
    }

    private static string GetShapeTestSourceDirectory()
    {
        var generatedSamplesDirectory = Directory.GetParent(GeneratedCodeSampleArtifacts.SamplesDirectory) ??
            throw new InvalidOperationException("Generated sample directory has no parent.");
        var repositoryRoot = generatedSamplesDirectory.Parent?.FullName ??
            throw new InvalidOperationException("Generated sample directory has no repository root.");

        return Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator.Tests");
    }
}
