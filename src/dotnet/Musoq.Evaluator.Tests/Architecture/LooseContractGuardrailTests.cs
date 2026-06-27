using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.Tests.Architecture;

/// <summary>
/// Ratchets for loose internal contracts. Each ceiling freezes the current
/// state so later changes can only tighten it. Lower the constants when real reductions land.
/// Baseline captured 2026-05-29 on feature/runtime_v2_optimizations_architecture; ceilings
/// tightened after the centralized build-key, optimizer-context, dynamic-boundary, and
/// unsupported-shape contract work landed.
/// </summary>
[TestClass]
public sealed class LooseContractGuardrailTests
{
    private const int BuildItemsInlineKeyCeiling = 46;
    private const int ExternalRawBuildItemAccessCeiling = 1;
    private const int OptimizationContextPropertiesUsageCeiling = 3;
    private const int ExecutionIrDynamicObjectBoundaryCeiling = 8;
    private const int UnsupportedShapeDiagnosticCeiling = 26;

    private static readonly Regex UpperSnakeStringLiteral = new("\"[A-Z][A-Z0-9_]{2,}\"", RegexOptions.Compiled);
    private static readonly Regex RawDictionaryAccess = new("\\b\\w+\\[\"[A-Z][A-Z0-9_]{2,}\"\\]", RegexOptions.Compiled);
    private static readonly Regex OptimizationContextProperties = new("context\\.Properties", RegexOptions.Compiled);
    private static readonly Regex DynamicObjectBoundary = new(
        "ExpandoObject|System\\.Dynamic|IDictionary<string, ?object>|IReadOnlyDictionary<string, ?object>",
        RegexOptions.Compiled);
    private static readonly Regex NotSupportedException = new("NotSupportedException", RegexOptions.Compiled);

    [TestMethod]
    public void BuildItems_InlineStringKeys_ShouldStayWithinBaselineCeiling()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var buildItemsFiles = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Converter/Build",
            "BuildItems*.cs");

        var distinctKeys = RepositorySourceScan.DistinctMatchCount(buildItemsFiles, UpperSnakeStringLiteral);

        Assert.IsLessThanOrEqualTo(
            BuildItemsInlineKeyCeiling,
            distinctKeys,
            $"Inline BuildItems string keys grew to {distinctKeys} (ceiling {BuildItemsInlineKeyCeiling}). " +
            "New build artifacts must flow through the centralized key registry instead of inline literals.");
    }

    [TestMethod]
    public void BuildItems_ExternalRawDictionaryAccess_ShouldStayWithinBaselineCeiling()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan
            .ProductionSourceFiles(repositoryRoot, "Musoq.Converter", "Musoq.Evaluator")
            .Where(file => !System.IO.Path.GetFileName(file).StartsWith("BuildItems", System.StringComparison.Ordinal));

        var accessSites = RepositorySourceScan.CountMatchingLines(files, RawDictionaryAccess);

        Assert.IsLessThanOrEqualTo(
            ExternalRawBuildItemAccessCeiling,
            accessSites,
            $"Direct dictionary access against build artifacts grew to {accessSites} site(s) " +
            $"(ceiling {ExternalRawBuildItemAccessCeiling}). Use typed BuildItems accessors instead.");
    }

    [TestMethod]
    public void OptimizationContext_PropertiesStringKeyUsage_ShouldStayWithinBaselineCeiling()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan.ProductionSourceFiles(repositoryRoot, "Musoq.Evaluator");

        var usages = RepositorySourceScan.CountMatchingLines(files, OptimizationContextProperties);

        Assert.IsLessThanOrEqualTo(
            OptimizationContextPropertiesUsageCeiling,
            usages,
            $"Stringly-typed OptimizationContext.Properties reads grew to {usages} (ceiling " +
            $"{OptimizationContextPropertiesUsageCeiling}). Move pass options into typed optimizer context state.");
    }

    [TestMethod]
    public void ExecutionIr_DynamicObjectBoundaryFamilies_ShouldStayWithinBaselineCeiling()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan.FilesUnder(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR", "*.cs");

        var boundaries = RepositorySourceScan.CountMatchingLines(files, DynamicObjectBoundary);

        Assert.IsLessThanOrEqualTo(
            ExecutionIrDynamicObjectBoundaryCeiling,
            boundaries,
            $"Dynamic/object boundary checks in IR grew to {boundaries} (ceiling " +
            $"{ExecutionIrDynamicObjectBoundaryCeiling}). Route new boundary checks through the boundary classifier.");
    }

    [TestMethod]
    public void ExecutionAndRendering_UnsupportedShapeDiagnostics_ShouldStayWithinBaselineCeiling()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan
            .FilesUnder(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/Execution", "*.cs")
            .Concat(RepositorySourceScan.FilesUnder(repositoryRoot, "src/dotnet/Musoq.Evaluator/IR/CodeGeneration", "*.cs"));

        var diagnostics = RepositorySourceScan.CountMatchingLines(files, NotSupportedException);

        Assert.IsLessThanOrEqualTo(
            UnsupportedShapeDiagnosticCeiling,
            diagnostics,
            $"Scattered unsupported-shape NotSupportedException sites grew to {diagnostics} (ceiling " +
            $"{UnsupportedShapeDiagnosticCeiling}). Route unsupported-shape reporting through shared helpers.");
    }

    [TestMethod]
    public void OptimizerPassPipelines_ShouldMatchFrozenOrdering()
    {
        AssertPassOrder(
            PreLogicalNormalizationGroup.Passes,
            "DistinctToGroupByNormalizationPass",
            "SubqueryToCteNormalizationPass");

        AssertPassOrder(
            LogicalNormalizationGroup.Passes,
            "LogicalConstantFoldingPass",
            "LogicalSourceAliasAnalysisPass");

        AssertPassOrder(
            LogicalOptimizationGroup.Passes,
            "DeadCteEliminationLogicalPass");

        AssertPassOrder(
            PhysicalOptimizationGroup.Passes,
            "SourcePredicateMetadataPass",
            "SourceProjectionMetadataPass",
            "ProjectionPruningPhysicalPass",
            "AggregateStrategySelectionPass",
            "PredicateMovementPhysicalPass",
            "JoinStrategySelectionPass",
            "OrderingStrategySelectionPass",
            "WindowMaterializationPass",
            "SourcePredicatePhysicalRewritePass",
            "SourcePlanPhysicalRewritePass");

        AssertPassOrder(
            ExecutionIrOptimizationGroup.Passes,
            "SingleUsePipelineFusionPass",
            "CteReadOnceFusionPass",
            "CteSidecarIndexLoweringPass",
            "MethodTargetReusePass",
            "FieldExpressionHoistingPass",
            "ExpressionCseHoistingPass",
            "CapacityHintPass",
            "MethodTargetReusePass");

        AssertPassOrder(
            CodegenReadabilityGroup.Passes,
            "DeterministicMemberOrderingPass",
            "LocalDeclarationNormalizationPass",
            "DeadTemporaryCleanupPass",
            "ControlFlowNormalizationPass",
            "HelperExtractionReadabilityPass",
            "ReadabilityDecisionTracePass");
    }

    [TestMethod]
    public void ExecutionIrOptimizationGroup_ShouldRunMethodTargetReuseExactlyTwice()
    {
        var methodTargetReuseRuns = ExecutionIrOptimizationGroup.Passes
            .Count(pass => pass.GetType().Name == "MethodTargetReusePass");

        Assert.AreEqual(
            2,
            methodTargetReuseRuns,
            "MethodTargetReusePass is intentionally run twice: once before CSE/capacity rewrites and once after " +
            "so reusable targets cover expressions those rewrites introduce. Keep both runs intentional.");
    }

    private static void AssertPassOrder<TPlan>(
        IReadOnlyList<IPlanOptimizationPass<TPlan>> passes,
        params string[] expectedTypeNames)
    {
        var actual = passes.Select(pass => pass.GetType().Name).ToArray();
        CollectionAssert.AreEqual(
            expectedTypeNames,
            actual,
            $"Optimizer pass ordering changed. Expected [{string.Join(", ", expectedTypeNames)}] " +
            $"but found [{string.Join(", ", actual)}].");
    }
}
