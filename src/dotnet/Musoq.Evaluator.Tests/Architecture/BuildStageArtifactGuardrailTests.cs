using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public class BuildStageArtifactGuardrailTests
{
    private static readonly string[] StageArtifactRecords =
    [
        "ParseBuildArtifacts",
        "SemanticBuildArtifacts",
        "PlanningBuildArtifacts",
        "ExecutionBuildArtifacts",
        "RenderingBuildArtifacts",
        "CompilationBuildArtifacts"
    ];

    [TestMethod]
    public void BuildStageArtifacts_ShouldDeclareEveryTypedStageRecord()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var artifactsDirectory = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Converter", "Build");
        var source = string.Concat(
            Directory
                .EnumerateFiles(artifactsDirectory, "*BuildArtifacts.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        var missing = StageArtifactRecords
            .Where(record => !source.Contains($"record {record}"))
            .ToArray();

        Assert.IsEmpty(
            missing,
            $"Build stage artifact files must declare a record for each build stage. Missing: {string.Join(", ", missing)}");
    }

    [TestMethod]
    public void TransformPipeline_ShouldConsumeEveryTypedStageArtifact()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var pipelineSource = string.Concat(
            RepositorySourceScan
                .FilesUnder(repositoryRoot, "src/dotnet/Musoq.Converter/Build", "*.cs")
                .Select(File.ReadAllText));

        var unused = StageArtifactRecords
            .Where(record => !pipelineSource.Contains(record))
            .ToArray();

        Assert.IsEmpty(
            unused,
            $"Every typed stage artifact must be produced or consumed by the transform pipeline. Unused: {string.Join(", ", unused)}");
    }
}
