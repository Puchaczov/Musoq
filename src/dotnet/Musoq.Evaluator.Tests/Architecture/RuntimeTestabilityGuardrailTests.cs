using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed partial class RuntimeTestabilityGuardrailTests
{
    private const int PhysicalToExecutionPrivateRecordBaseline = 7;

    private static readonly string[] RendererDispatchNodeBaseline =
    [
        "ExecutionAdaptExpando",
        "ExecutionAggregateCapturedValueSet",
        "ExecutionAggregateSet",
        "ExecutionAppendExistingRow",
        "ExecutionAppendRecord",
        "ExecutionAppendRow",
        "ExecutionArrayAssign",
        "ExecutionAsOfProbe",
        "ExecutionAssign",
        "ExecutionBreak",
        "ExecutionComputeOffsetWindow",
        "ExecutionComputePluginWindow",
        "ExecutionComputeRankingWindow",
        "ExecutionContinue",
        "ExecutionContinueIf",
        "ExecutionCreateAggregateContext",
        "ExecutionCreateAggregateLibrary",
        "ExecutionCreateAsOfIndex",
        "ExecutionCreateBooleanArray",
        "ExecutionCreateBoundedRecordList",
        "ExecutionCreateGeneratedRow",
        "ExecutionCreateHash",
        "ExecutionCreateHashPayload",
        "ExecutionCreateKeySet",
        "ExecutionCreateObject",
        "ExecutionCreateRangeIndex",
        "ExecutionCreateRecordList",
        "ExecutionCreateSingleKeyAggregateContext",
        "ExecutionCreateTable",
        "ExecutionCreateValuesRows",
        "ExecutionCreateValueTupleAggregateContext",
        "ExecutionDistinctTable",
        "ExecutionEnsureAggregateGroup",
        "ExecutionEnsureTableCapacity",
        "ExecutionEnumerableSource",
        "ExecutionForEach",
        "ExecutionForEachIndexed",
        "ExecutionForEachWithOrdinality",
        "ExecutionFusedCteProducer",
        "ExecutionGetOrAddSingleKeyAggregateGroup",
        "ExecutionGetOrAddValueTupleAggregateGroup",
        "ExecutionHashAdd",
        "ExecutionHashProbe",
        "ExecutionIf",
        "ExecutionInterpretSource",
        "ExecutionKeySetAdd",
        "ExecutionKeySetProbe",
        "ExecutionLet",
        "ExecutionLoadCteIndex",
        "ExecutionMaterializeExpandoList",
        "ExecutionMaterializeFilteredList",
        "ExecutionMaterializeList",
        "ExecutionMaterializeRecordListToTable",
        "ExecutionOrderRecordList",
        "ExecutionParallelBlock",
        "ExecutionParallelFilterProjectLoop",
        "ExecutionParallelSingleKeyAggregateLoop",
        "ExecutionProjectTable",
        "ExecutionRangeProbe",
        "ExecutionRelatedCtePhase",
        "ExecutionReturnDesc",
        "ExecutionReturnTable",
        "ExecutionScopedBlock",
        "ExecutionSetOperation",
        "ExecutionSkipTable",
        "ExecutionSliceTable",
        "ExecutionSortTable",
        "ExecutionSourceScan",
        "ExecutionStoreCteIndex",
        "ExecutionStoreTable",
        "ExecutionTakeTable",
        "ExecutionTopNTable",
        "ExecutionTopOffsetTable",
        "ExecutionWindowAggregateKernel",
        "ExecutionWindowKernelPlan"
    ];

    [TestMethod]
    public void PhysicalToExecutionPlanBuilder_ShouldNotAddPrivatePlannerRecords()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var files = RepositorySourceScan.FilesUnder(
            repositoryRoot,
            "src/dotnet/Musoq.Evaluator/IR/Execution",
            "PhysicalToExecutionPlanBuilder*.cs");
        var declarations = files
            .SelectMany(file => File
                .ReadLines(file)
                .Select((line, index) => new
                {
                    File = RepositorySourceScan.ToRelative(repositoryRoot, file),
                    Line = index + 1,
                    Text = line.Trim()
                }))
            .Where(static item => PrivateRecordPattern().IsMatch(item.Text))
            .Select(static item => $"{item.File}:{item.Line}: {item.Text}")
            .ToArray();

        Assert.IsTrue(
            declarations.Length <= PhysicalToExecutionPrivateRecordBaseline,
            "Do not add new private planner/result records inside PhysicalToExecutionPlanBuilder*. " +
            "Extract new models under IR/Execution/Lowering and test them directly. Current declarations: " +
            string.Join(Environment.NewLine, declarations));
    }

    [TestMethod]
    public void RendererDispatchClassifier_ShouldStayAlignedWithExplicitTestBaseline()
    {
        var current = ExecutionNodeRegistry.Descriptors
            .Where(static descriptor => descriptor.RendererFamily != ExecutionRendererNodeFamily.Unsupported)
            .Select(static descriptor => descriptor.NodeType.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expected = RendererDispatchNodeBaseline
            .Order(StringComparer.Ordinal)
            .ToArray();
        var unexpected = current.Except(expected, StringComparer.Ordinal).ToArray();
        var missing = expected.Except(current, StringComparer.Ordinal).ToArray();

        Assert.IsEmpty(
            unexpected,
            "New renderer dispatch branches need focused dispatch/capability tests before updating this baseline: " +
            string.Join(", ", unexpected));
        Assert.IsEmpty(
            missing,
            "Renderer dispatch baseline contains nodes no longer classified by the dispatcher: " +
            string.Join(", ", missing));
    }

    [TestMethod]
    public void ExecutionNodeRegistry_ShouldCoverRewriterNodeInventory()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var rewriterFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "ExecutionIrRewriter.cs");
        var rewriterNodes = RewriterNodePattern()
            .Matches(File.ReadAllText(rewriterFile))
            .Select(static match => match.Groups["node"].Value)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var registeredNodes = ExecutionNodeRegistry.Descriptors
            .Select(static descriptor => descriptor.NodeType.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var missing = rewriterNodes.Except(registeredNodes, StringComparer.Ordinal).ToArray();

        Assert.IsEmpty(
            missing,
            "Execution node registry must cover nodes that the rewriter handles so analysis/traversal inventory stays centralized: " +
            string.Join(", ", missing));
    }

    [TestMethod]
    public void RenderableExecutionNodes_ShouldHavePrinterDispatchCoverage()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var printerFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "PlanPrinterNodeDispatch.cs");
        var printerSource = File.ReadAllText(printerFile);
        var missing = ExecutionNodeRegistry.Descriptors
            .Where(static descriptor => descriptor.RendererFamily != ExecutionRendererNodeFamily.Unsupported)
            .Select(static descriptor => descriptor.NodeType.Name)
            .Where(nodeName => !PrinterCasePattern(nodeName).IsMatch(printerSource))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            missing,
            "Renderable execution nodes must have an explicit plan-printer branch: " +
            string.Join(", ", missing));
    }

    [TestMethod]
    public void RendererSources_ShouldNotCallPlanningOrLowering()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var rendererFiles = RepositorySourceScan
            .ProductionSourceFiles(repositoryRoot, "Musoq.Evaluator")
            .Where(static file => Path.GetFileName(file).Contains("Renderer", StringComparison.Ordinal))
            .ToArray();
        string[] forbiddenMarkers =
        [
            "using Musoq.Evaluator.IR.Planning",
            "Musoq.Evaluator.IR.Planning.",
            "using Musoq.Evaluator.IR.Execution.Lowering",
            "Musoq.Evaluator.IR.Execution.Lowering.",
            "new QueryPlanner(",
            "new ExecutionStrategyPlanner(",
            "new PhysicalToExecutionPlanBuilder("
        ];
        var offenders = rendererFiles
            .Where(file => forbiddenMarkers.Any(marker => File.ReadAllText(file).Contains(marker, StringComparison.Ordinal)))
            .Select(file => RepositorySourceScan.ToRelative(repositoryRoot, file))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.IsEmpty(
            offenders,
            "Renderers must consume execution nodes only; planning/lowering orchestration belongs before rendering: " +
            string.Join(", ", offenders));
    }

    [TestMethod]
    public void ExecutionRendererNodeDispatch_ShouldUseRegistryWithoutConcreteNodeInventory()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var dispatchFile = Path.Combine(
            repositoryRoot,
            "src", "dotnet", "Musoq.Evaluator", "IR", "Execution", "ExecutionCSharpRenderer.NodeDispatch.cs");
        var dispatchText = File.ReadAllText(dispatchFile);
        var concreteNodeMentions = ExecutionNodeRegistry.Descriptors
            .Select(static descriptor => descriptor.NodeType.Name)
            .Where(nodeName => dispatchText.Contains(nodeName, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Contains("ExecutionNodeRegistry.GetRendererFamily(node)", dispatchText);
        Assert.IsEmpty(
            concreteNodeMentions,
            "ExecutionCSharpRenderer.NodeDispatch must route through ExecutionNodeRegistry instead of owning a concrete node inventory: " +
            string.Join(", ", concreteNodeMentions));
    }

    [GeneratedRegex(@"private\s+(sealed\s+|readonly\s+|static\s+)*record\b")]
    private static partial Regex PrivateRecordPattern();

    [GeneratedRegex(@"(?<node>Execution[A-Za-z0-9]+)\s+[A-Za-z0-9]+\s+=>\s+Rewrite")]
    private static partial Regex RewriterNodePattern();

    private static Regex PrinterCasePattern(string nodeName)
    {
        return new Regex($@"\b(case|or)\s+{Regex.Escape(nodeName)}\b", RegexOptions.CultureInvariant);
    }
}
