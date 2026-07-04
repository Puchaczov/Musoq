using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using PlanProperties = Musoq.Evaluator.IR.Planning.PlanProperties;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class RuleBasedOptimizationTraceCoverageTests : BasicEntityTestBase
{
    [TestMethod]
    public void OptimizerTraceText_WhenAnalyzeBuildRunsRuleBasedPipeline_ShouldIncludeAllStagesWithReasons()
    {
        var buildItems = CreateBuildItems<UsedColumnsOrUsedWhereEntity>(
            "select e.Name from #A.Entities() e where e.Population > 100 order by e.Name take 2");
        var traceText = buildItems.OptimizerTraceText;

        Assert.IsNotNull(traceText);
        Assert.Contains("OptimizerTrace", traceText);
        Assert.Contains("PreLogicalNormalization [DistinctToGroupByNormalization]", traceText);
        Assert.Contains("PreLogicalNormalization [SubqueryToCteNormalization]", traceText);
        Assert.Contains("LogicalNormalization [LogicalConstantFolding]", traceText);
        Assert.Contains("LogicalOptimization [DeadCteElimination]", traceText);
        Assert.Contains("PhysicalOptimization [SourcePredicateMetadata]", traceText);
        Assert.Contains("PhysicalOptimization [ProjectionPruning]", traceText);
        Assert.Contains("PhysicalOptimization [PredicateMovement]", traceText);
        Assert.Contains("PhysicalOptimization [SourcePredicatePhysicalRewrite]", traceText);
        Assert.Contains("ExecutionIrOptimization [ExpressionCseHoisting]", traceText);
        Assert.Contains("ExecutionIrOptimization [CapacityHints]", traceText);
        Assert.Contains("CodegenReadability [HelperExtractionReadability]", traceText);
        Assert.Contains("changed: no -", traceText);
        Assert.Contains("changed: yes -", traceText);
        AssertTraceLinesHaveReasons(traceText);
    }

    [TestMethod]
    public void OptimizerTraceText_WhenSubqueryNormalizationChangesTree_ShouldReportGeneratedKinds()
    {
        var buildItems = CreateBuildItems<BasicEntity>("""
            select a.City, (
                select b.City from #B.entities() b
                where b.Country = 'FRANCE'
            ) as MatchCity
            from #A.entities() a
            where exists (
                select c.City from #C.entities() c
                where c.Country = a.Country
            )
            """);
        var traceText = buildItems.OptimizerTraceText;

        Assert.IsNotNull(traceText);
        Assert.Contains("PreLogicalNormalization [SubqueryToCteNormalization]", traceText);
        Assert.Contains("changed: yes", traceText);
        Assert.Contains("Predicate=1", traceText);
        Assert.Contains("Scalar=1", traceText);
    }

    [TestMethod]
    public void PhysicalOptimizerTrace_WhenRuleBasedPassesSkip_ShouldNameSkippedFamilies()
    {
        var plan = new PhysicalValuesScanNode(
            "v",
            [],
            new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]));

        var result = new PhysicalOptimizer().Optimize(
            plan,
            CreateEmptyProperties(),
            new CompilationOptions(),
            ConservativeTestPlanningShapeResolver.Instance);

        AssertTraceReason(
            result.Trace.Entries,
            "SourcePredicateMetadata",
            "No source predicate metadata was applied.");
        AssertTraceReason(
            result.Trace.Entries,
            "SourceProjectionMetadata",
            "No source projection metadata was applied.");
        AssertTraceReason(
            result.Trace.Entries,
            "ProjectionPruning",
            "No simple projection chains were safe to prune.");
        AssertTraceReason(
            result.Trace.Entries,
            "PredicateMovement",
            "No physical predicate movements were applied.");
        AssertTraceReason(
            result.Trace.Entries,
            "SourcePredicatePhysicalRewrite",
            "No accepted source predicate conjuncts were removed from the physical plan.");
        AssertTraceReason(
            result.Trace.Entries,
            "SourcePlanPhysicalRewrite",
            "No source-local order or slice operations were removed from the physical plan.");
    }

    private static void AssertTraceLinesHaveReasons(string traceText)
    {
        var traceLines = traceText
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Where(static line => line.StartsWith("  ", StringComparison.Ordinal))
            .ToArray();

        Assert.IsTrue(traceLines.Length > 0, traceText);
        Assert.IsTrue(
            traceLines.All(static line =>
                line.Contains(" - ", StringComparison.Ordinal) &&
                !line.EndsWith(" - ", StringComparison.Ordinal)),
            traceText);
    }

    private static void AssertTraceReason(
        IReadOnlyList<OptimizationTraceEntry> entries,
        string passName,
        string expectedReason)
    {
        var entry = entries.Single(entry => string.Equals(entry.PassName, passName, StringComparison.Ordinal));

        Assert.IsFalse(entry.IsChanged);
        Assert.AreEqual("NoChange", entry.Outcome);
        Assert.Contains(expectedReason, entry.Reason);
    }

    private static PlanProperties CreateEmptyProperties()
    {
        return PlanPropertiesTestFactory.CreateEmpty();
    }
}
