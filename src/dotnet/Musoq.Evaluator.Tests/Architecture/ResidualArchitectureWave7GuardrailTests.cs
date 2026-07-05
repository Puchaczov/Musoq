using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave7GuardrailTests
{
    [TestMethod]
    public void SourceAndJoinLoweringModels_ShouldNotLiveInBuilderPrivateTypePartial()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var builderModelFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.Types.SourcesAndJoins.cs");
        var sourceLoweringDirectory = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "Lowering", "Sources");

        Assert.IsFalse(File.Exists(builderModelFile));
        Assert.IsTrue(File.Exists(Path.Combine(sourceLoweringDirectory, "ApplyChainLoweringModels.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(sourceLoweringDirectory, "SingleKeyAggregateExecutionSourceModel.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(sourceLoweringDirectory, "ApplyChainSourceCollector.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(sourceLoweringDirectory, "JoinSourceLookupBuilder.cs")));

        var builderPrivateModelOffenders = RepositorySourceScan
            .FilesUnder(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Execution", "PhysicalToExecutionPlanBuilder*.cs")
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(static item =>
                item.Text.Contains("private sealed record ApplyChain", StringComparison.Ordinal) ||
                item.Text.Contains("private sealed record SingleKeyAggregateExecutionSource", StringComparison.Ordinal))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();

        Assert.IsEmpty(
            builderPrivateModelOffenders,
            "Source/join lowering model records should stay under IR/Execution/Lowering/Sources: " +
            string.Join(Environment.NewLine, builderPrivateModelOffenders));
    }

    [TestMethod]
    public void SourceLookupAssembly_ShouldUseLoweringHelperInsteadOfBuilderLocalHelpers()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var crossApplyText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.CrossApplyChains.cs"));
        var sidecarRewriteText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.SidecarJoinPipeline.Rewrite.cs"));
        var sidecarTableText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.SidecarJoinPipeline.Table.cs"));

        Assert.Contains("ApplyChainSourceCollector", crossApplyText);
        Assert.Contains("JoinSourceLookupBuilder.Extend", crossApplyText);
        Assert.DoesNotContain("CloneSourceLookup", sidecarRewriteText);
        Assert.DoesNotContain("AddSourceShape", sidecarRewriteText);
        Assert.DoesNotContain("AddJoinSourceShapes", sidecarRewriteText);
        Assert.Contains("JoinSourceLookupBuilder.Clone", sidecarTableText);
        Assert.Contains("JoinSourceLookupBuilder.TryAdd", sidecarTableText);
        Assert.Contains("JoinSourceLookupBuilder.AddShapes", sidecarTableText);
    }
}
