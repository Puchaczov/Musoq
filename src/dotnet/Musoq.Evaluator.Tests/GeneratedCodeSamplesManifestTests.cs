using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tests.Architecture;
using Musoq.Evaluator.Tests.Components;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class GeneratedCodeSamplesManifestTests
{
    private const string ManifestRelativePath = "src/dotnet/Musoq.Evaluator.Tests/GeneratedCodeSamplesManifest.txt";

    [TestMethod]
    public void Catalog_WhenGenerated_ShouldMatchTrackedManifest()
    {
        var expected = ReadManifestLines(GetManifestPath());
        var actual = CreateManifestLines();

        CollectionAssert.AreEqual(
            expected,
            actual,
            $"Generated-code sample manifest is stale. Run {nameof(Refresh_Tracked_Generated_Code_Sample_Manifest)} intentionally when generated-code changes are expected.");
    }

    [TestMethod]
    [Ignore("Local manifest refresh utility. Run intentionally when generated-code changes are expected.")]
    public void Refresh_Tracked_Generated_Code_Sample_Manifest()
    {
        File.WriteAllLines(GetManifestPath(), CreateManifestLinesWithHeader(), new UTF8Encoding(false));
    }

    internal static string[] CreateManifestLines()
    {
        var loggerResolver = new TestsLoggerResolver();

        return GeneratedCodeSamplesCatalog.Samples
            .OrderBy(static sample => sample.FileName, StringComparer.Ordinal)
            .Select(sample =>
            {
                var generated = GeneratedCodeSampleArtifacts.Generate(sample, loggerResolver);
                var normalized = GeneratedCodeSampleArtifacts.NormalizeForComparison(generated);
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();

                return $"{sample.FileName}\t{sample.Category}\t{hash}";
            })
            .ToArray();
    }

    private static string[] CreateManifestLinesWithHeader()
    {
        return
        [
            "# Generated-code sample manifest.",
            "# Format: file-name<TAB>category<TAB>sha256(normalized generated sample)",
            "# Refresh intentionally through GeneratedCodeSamplesManifestTests.Refresh_Tracked_Generated_Code_Sample_Manifest.",
            .. CreateManifestLines()
        ];
    }

    private static string[] ReadManifestLines(string manifestPath)
    {
        return File.ReadAllLines(manifestPath)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Where(static line => !line.StartsWith("#", StringComparison.Ordinal))
            .ToArray();
    }

    private static string GetManifestPath()
    {
        return Path.Combine(RepositorySourceScan.RepositoryRoot(), ManifestRelativePath);
    }
}
