using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class BenchmarkSnapshotTests
{
    [TestMethod]
    public void ActiveCases_WhenRegenerated_ShouldMatchTrackedBenchmarkSnapshots()
    {
        var generated = BenchmarkSnapshotGenerator.Generate();
        foreach (var text in generated.Files.Values)
            _ = BenchmarkSnapshotSections.Parse(text);

        var output = Environment.GetEnvironmentVariable("MUSOQ_BENCHMARK_SNAPSHOT_OUTPUT");
        if (!string.IsNullOrWhiteSpace(output))
        {
            WritePreview(output, generated);
            return;
        }

        var directory = BenchmarkSnapshotGenerator.DirectoryPath;
        var manifestPath = Path.Combine(directory, BenchmarkSnapshotManifest.FileName);
        const string refresh = "Run scripts/benchmarks/Update-BenchmarkSnapshots.ps1 -Apply intentionally.";
        Assert.IsTrue(File.Exists(manifestPath), $"Benchmark snapshot manifest is missing. {refresh}");
        var manifest = File.ReadAllText(manifestPath);
        _ = BenchmarkSnapshotManifest.Parse(manifest);
        Assert.AreEqual(
            JsonSerializer.Serialize(generated.Manifest, ArtifactJson.Options) + "\n",
            manifest,
            $"Benchmark case mappings or workload definitions are stale. {refresh}"
        );
        CollectionAssert.AreEqual(
            generated.Files.Keys.ToArray(),
            Directory
                .GetFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(directory, path))
                .Order(StringComparer.Ordinal)
                .ToArray(),
            $"Benchmark snapshot files are missing or orphaned. {refresh}"
        );
        foreach (var (file, text) in generated.Files)
        {
            Assert.AreEqual(
                text,
                File.ReadAllText(Path.Combine(directory, file)),
                $"Benchmark snapshot '{file}' is stale. {refresh}"
            );
        }
    }

    private static void WritePreview(string output, GeneratedBenchmarkSnapshots generated)
    {
        var artifacts = Path.Combine(RepositoryPaths.FindRoot(), ".artifacts");
        var directory = Path.GetFullPath(output);
        var relative = Path.GetRelativePath(artifacts, directory);
        Assert.IsFalse(
            relative is "." or ".."
                || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                || Path.IsPathRooted(relative),
            "Benchmark snapshot previews must be written under .artifacts."
        );
        Assert.IsFalse(Directory.Exists(directory), "Use a new directory for each generated snapshot preview.");
        generated.WriteTo(directory);
    }
}
