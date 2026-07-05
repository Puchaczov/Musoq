using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class ResidualArchitectureWave9GuardrailTests
{
    [TestMethod]
    public void PhysicalToExecutionBuilderPartials_ShouldNotOwnPrivateLoweringTypes()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var builderTypePattern = new Regex(
            @"private\s+(?:sealed\s+)?(?:readonly\s+)?(?:record|class|enum|delegate)",
            RegexOptions.Compiled);
        var offenders = RepositorySourceScan
            .FilesUnder(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Execution", "PhysicalToExecutionPlanBuilder*.cs")
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(item => builderTypePattern.IsMatch(item.Text))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Execution lowering helper/model types should live under IR/Execution/Lowering, not as builder-private nested types: " +
            string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void LoweringCoordinators_ShouldNotAcceptWholePhysicalToExecutionBuilder()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var offenders = RepositorySourceScan
            .FilesUnder(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Execution/Lowering", "*.cs")
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(static item =>
                item.Text.Contains("PhysicalToExecutionPlanBuilder builder", StringComparison.Ordinal) ||
                item.Text.Contains("PhysicalToExecutionPlanBuilder)", StringComparison.Ordinal) ||
                item.Text.Contains("new ", StringComparison.Ordinal) &&
                item.Text.Contains("(this)", StringComparison.Ordinal))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Lowering collaborators should receive focused delegates/facts instead of the whole PhysicalToExecutionPlanBuilder: " +
            string.Join(Environment.NewLine, offenders));
    }

    [TestMethod]
    public void ExtractedBuilderModelTypes_ShouldLiveUnderLoweringFolders()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();

        Assert.IsTrue(File.Exists(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "Lowering", "ProjectionAndApply", "NestedApplyGeneratedRowPreservation.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "Lowering", "ProjectionAndApply", "RowPresenceSubstitutionRewriter.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "Lowering", "Windows", "WindowAggregateSourceField.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "Lowering", "Windows", "WindowPartitionSortUsage.cs")));
        Assert.IsTrue(File.Exists(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "Lowering", "Aggregates", "NestedTransitionBinding.cs")));
        Assert.IsFalse(File.Exists(Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.WindowPartitionSortUsage.cs")));
    }
}
