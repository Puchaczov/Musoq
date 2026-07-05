using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class CteSidecarGlobalSupportGuardrailTests
{
    private static readonly string[] DelegatedStageTerms =
    [
        "ASOF",
        "Asof",
        "AsOf",
        "FULL",
        "Full",
        "Apply",
        "APPLY",
        "Cross",
        "CROSS"
    ];

    [TestMethod]
    public void CteSidecarPipelineStages_ShouldKeepGlobalJoinFamiliesModeledAndDelegated()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var modelText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Execution",
            "Lowering",
            "Ctes",
            "CteLoweringModel.cs"));
        var chainText = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Evaluator",
            "IR",
            "Execution",
            "PhysicalToExecutionPlanBuilder.SingleUseSidecarJoinChains.cs"));

        foreach (var stageKind in new[] { "StandardJoin", "AsOfJoin", "Apply", "CrossJoin" })
            Assert.Contains(stageKind, modelText);

        foreach (var requiredMapping in new[]
                 {
                     "PhysicalNestedLoopApplyNode => SidecarJoinPipelineStageKind.Apply",
                     "PhysicalNestedLoopJoinNode { Kind: JoinKind.Cross } => SidecarJoinPipelineStageKind.CrossJoin",
                     "PhysicalNestedLoopJoinNode { Kind: JoinKind.AsofInner or JoinKind.AsofLeft } => SidecarJoinPipelineStageKind.AsOfJoin",
                     "PhysicalNestedLoopJoinNode or PhysicalHashJoinNode or PhysicalSortMergeJoinNode => SidecarJoinPipelineStageKind.StandardJoin",
                     "BuildDelegatedSidecarEnabledMultiStatementTable",
                     "session.WithSidecarJoinPipelineSuppressed()"
                 })
        {
            Assert.Contains(requiredMapping, chainText);
        }
    }

    [TestMethod]
    public void CteSidecarPipelineStages_ShouldKeepGlobalRegressionFamilies()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var asOfText = ReadConverterTest(repositoryRoot, "QueryInspectionTests.CteSidecarGlobalSupport.AsOf.cs");
        var crossApplyText = ReadConverterTest(repositoryRoot, "QueryInspectionTests.CteSidecarGlobalSupport.CrossApply.cs");
        var fullOuterText = ReadConverterTest(repositoryRoot, "QueryInspectionTests.CteSidecarGlobalSupport.FullOuter.cs");

        foreach (var requiredTestMarker in new[]
                 {
                     "CteSidecarIndexesAreEnabledForAsOfBackwardLookupWithPartition",
                     "CteSidecarIndexesAreEnabledForAsOfForwardLookup",
                     "CteSidecarIndexesAreEnabledForAsOfLeftNoMatch",
                     "CteSidecarIndexesAreEnabledForAsOfTieBreak",
                     "CteSidecarIndexesAreEnabledForCteBackedAsOfJoin"
                 })
        {
            Assert.Contains(requiredTestMarker, asOfText);
        }

        foreach (var requiredTestMarker in new[]
                 {
                     "CteSidecarIndexesAreEnabledForCrossJoinValues",
                     "CteSidecarIndexesAreEnabledForCrossApplySchemaSource",
                     "CteSidecarIndexesAreEnabledForOuterApplyNoMatches",
                     "CteSidecarIndexesAreEnabledForApplyWithOrdinality"
                 })
        {
            Assert.Contains(requiredTestMarker, crossApplyText);
        }

        foreach (var requiredTestMarker in new[]
                 {
                     "CteSidecarIndexesAreEnabledForFullOuterPresencePredicates",
                     "CteSidecarIndexesAreEnabledForFullOuterResidualPredicate",
                     "CteSidecarIndexesAreEnabledForFullOuterJoin"
                 })
        {
            Assert.Contains(requiredTestMarker, fullOuterText);
        }

        Assert.Contains("useCteSidecarIndexes: true", crossApplyText);
    }

    [TestMethod]
    public void CteSidecarPipelineStages_ShouldNotHardRejectDelegatedGlobalFamilies()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var guardedFiles = new[]
        {
            Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PhysicalToExecutionPlanBuilder.SingleUseSidecarJoinChains.cs"),
            Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "Lowering", "Ctes", "SidecarJoinCteLowerer.cs")
        };

        var offenders = MatchingLines(guardedFiles, repositoryRoot, line =>
            line.Contains("Unsupported", StringComparison.OrdinalIgnoreCase) &&
            DelegatedStageTerms.Any(term => line.Contains(term, StringComparison.Ordinal)));

        Assert.IsEmpty(offenders, "Sidecar pipeline global join families must delegate to standard lowering instead of hard-rejecting: " + string.Join(Environment.NewLine, offenders));
    }

    private static string ReadConverterTest(string repositoryRoot, string fileName)
    {
        return File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "dotnet",
            "Musoq.Converter.Tests",
            fileName));
    }

    private static string[] MatchingLines(
        IEnumerable<string> files,
        string repositoryRoot,
        Func<string, bool> predicate)
    {
        return files
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(item => predicate(item.Text))
            .Select(item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();
    }
}
