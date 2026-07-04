using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Logical;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class LogicalOptimizerTests
{
    [TestMethod]
    public void Optimize_WhenNoLogicalRewriteApplies_ShouldReturnInitialPlanAsOptimizedPlan()
    {
        var initial = new ValuesScanNode(
            "v",
            [],
            new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]));

        var result = new LogicalOptimizer().Optimize(initial);

        Assert.AreSame(initial, result.InitialPlan);
        Assert.AreSame(initial, result.OptimizedPlan);
        Assert.HasCount(3, result.Trace.Entries);
        Assert.AreEqual("LogicalConstantFolding", result.Trace.Entries[0].PassName);
        Assert.AreEqual("LogicalSourceAliasAnalysis", result.Trace.Entries[1].PassName);
        Assert.AreEqual("DeadCteElimination", result.Trace.Entries[2].PassName);
        Assert.IsFalse(result.Trace.Entries.Any(static entry => entry.IsChanged));
        AssertTraceEntriesAreMeaningful(result.Trace.Entries);
    }

    [TestMethod]
    public void Optimize_WhenCteDefinitionIsUnreachable_ShouldRemoveIt()
    {
        var schema = new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]);
        var usedDefinitionPlan = new ValuesScanNode("used", [], schema);
        var unusedDefinitionPlan = new ValuesScanNode("unused", [], schema);
        var query = new CteRefNode("used", "u", schema);
        var initial = new CteNode(
            [
                new CteDefinition("used", usedDefinitionPlan),
                new CteDefinition("unused", unusedDefinitionPlan)
            ],
            query);

        var result = new LogicalOptimizer().Optimize(initial);

        Assert.IsInstanceOfType<CteNode>(result.OptimizedPlan);
        var optimized = (CteNode)result.OptimizedPlan;
        Assert.HasCount(1, optimized.Definitions);
        Assert.AreEqual("used", optimized.Definitions[0].Name);
        Assert.AreSame(usedDefinitionPlan, optimized.Definitions[0].Plan);
        Assert.AreSame(query, optimized.Query);
        var deadCteEntry = result.Trace.Entries.Single(static entry => entry.PassName == "DeadCteElimination");
        Assert.IsTrue(deadCteEntry.IsChanged);
        Assert.Contains("Removed 1 dead CTE definition.", deadCteEntry.Reason);
    }

    [TestMethod]
    public void Optimize_WhenCteDefinitionIsTransitivelyReachable_ShouldKeepIt()
    {
        var schema = new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]);
        var baseDefinitionPlan = new ValuesScanNode("base", [], schema);
        var middleDefinitionPlan = new CteRefNode("base", "b", schema);
        var query = new CteRefNode("middle", "m", schema);
        var initial = new CteNode(
            [
                new CteDefinition("base", baseDefinitionPlan),
                new CteDefinition("middle", middleDefinitionPlan)
            ],
            query);

        var result = new LogicalOptimizer().Optimize(initial);

        Assert.AreSame(initial, result.OptimizedPlan);
        Assert.IsFalse(result.Trace.Entries.Single(static entry => entry.PassName == "DeadCteElimination").IsChanged);
    }

    [TestMethod]
    public void Optimize_WhenCteIsReferencedByExpression_ShouldKeepIt()
    {
        var schema = new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]);
        var usedDefinitionPlan = new ValuesScanNode("used", [], schema);
        var unusedDefinitionPlan = new ValuesScanNode("unused", [], schema);
        var source = new ValuesScanNode("input", [], schema);
        var query = new ProjectNode(
            [new ProjectedField("Value", new CteTableRef("used"), 0)],
            source);
        var initial = new CteNode(
            [
                new CteDefinition("used", usedDefinitionPlan),
                new CteDefinition("unused", unusedDefinitionPlan)
            ],
            query);

        var result = new LogicalOptimizer().Optimize(initial);

        Assert.IsInstanceOfType<CteNode>(result.OptimizedPlan);
        var optimized = (CteNode)result.OptimizedPlan;
        Assert.HasCount(1, optimized.Definitions);
        Assert.AreEqual("used", optimized.Definitions[0].Name);
    }

    [TestMethod]
    public void Optimize_WhenAllCteDefinitionsAreUnreachable_ShouldRemoveCteWrapper()
    {
        var schema = new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]);
        var unusedDefinitionPlan = new ValuesScanNode("unused", [], schema);
        var query = new ValuesScanNode("query", [], schema);
        var initial = new CteNode(
            [new CteDefinition("unused", unusedDefinitionPlan)],
            query);

        var result = new LogicalOptimizer().Optimize(initial);

        Assert.AreSame(query, result.OptimizedPlan);
        var deadCteEntry = result.Trace.Entries.Single(static entry => entry.PassName == "DeadCteElimination");
        Assert.IsTrue(deadCteEntry.IsChanged);
        Assert.Contains("Removed 1 dead CTE definition.", deadCteEntry.Reason);
    }

    [TestMethod]
    public void Optimize_WhenUnreachableCteContainsSourceScanAndAnalysisFactsAreStable_ShouldRemoveIt()
    {
        var schema = new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]);
        var unusedDefinitionPlan = new SchemaScanNode("#A", "entities", [], "a", schema, "a:1");
        var query = new ValuesScanNode("query", [], schema);
        var initial = new CteNode(
            [new CteDefinition("unused", unusedDefinitionPlan)],
            query);

        var result = new LogicalOptimizer().Optimize(initial);

        Assert.AreSame(query, result.OptimizedPlan);
        var deadCteEntry = result.Trace.Entries.Single(static entry => entry.PassName == "DeadCteElimination");
        Assert.IsTrue(deadCteEntry.IsChanged);
        Assert.Contains("source-bearing logical nodes", deadCteEntry.Reason);
        Assert.Contains("analysis facts: consumed 1", deadCteEntry.Reason);
    }

    [TestMethod]
    public void DeadCteElimination_WhenSourceScanFactsAreMissing_ShouldPreserveSourceBearingDefinition()
    {
        var schema = new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]);
        var unusedDefinitionPlan = new SchemaScanNode("#A", "entities", [], "a", schema, "a:1");
        var query = new ValuesScanNode("query", [], schema);
        var initial = new CteNode(
            [new CteDefinition("unused", unusedDefinitionPlan)],
            query);

        var result = new DeadCteEliminationLogicalPass().Optimize(
            initial,
            new OptimizationContext(OptimizationStage.LogicalOptimization));

        Assert.AreSame(initial, result.Plan);
        Assert.IsFalse(result.IsChanged);
    }

    [TestMethod]
    public void Optimize_WhenLogicalConstantExpressionIsPresent_ShouldFoldIt()
    {
        var input = new ValuesScanNode(
            "v",
            [],
            new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]));
        var initial = new ProjectNode(
            [
                new ProjectedField(
                    "Folded",
                    new BinaryOp(
                        BinaryOpKind.Add,
                        new Literal(1, typeof(int)),
                        new Literal(2, typeof(int)),
                        typeof(int)),
                    0)
            ],
            input);

        var result = new LogicalOptimizer().Optimize(initial);

        var optimized = (ProjectNode)result.OptimizedPlan;
        var literal = (Literal)optimized.Fields[0].Expression;
        Assert.AreEqual(3, literal.Value);

        var constantFoldingEntry = result.Trace.Entries.Single(static entry => entry.PassName == "LogicalConstantFolding");
        Assert.IsTrue(constantFoldingEntry.IsChanged);
        Assert.AreEqual("Folded 1 logical constant expression.", constantFoldingEntry.Reason);
    }

    [TestMethod]
    public void Optimize_WhenLogicalConstantFoldingIsDisabled_ShouldLeaveExpressionUnchanged()
    {
        var input = new ValuesScanNode(
            "v",
            [],
            new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]));
        var expression = new BinaryOp(
            BinaryOpKind.Add,
            new Literal(1, typeof(int)),
            new Literal(2, typeof(int)),
            typeof(int));
        var initial = new ProjectNode(
            [new ProjectedField("Folded", expression, 0)],
            input);

        var result = new LogicalOptimizer(enableConstantFolding: false).Optimize(initial);

        Assert.AreSame(initial, result.OptimizedPlan);
        var constantFoldingEntry = result.Trace.Entries.Single(static entry => entry.PassName == "LogicalConstantFolding");
        Assert.IsFalse(constantFoldingEntry.IsChanged);
        Assert.AreEqual("Logical constant folding is disabled by compilation options.", constantFoldingEntry.Reason);
    }

    [TestMethod]
    public void Optimize_WhenLogicalConstantDivisionByZeroIsPresent_ShouldReportDiagnosticWithSourceSpan()
    {
        var sourceText = new SourceText("SELECT 1 / 0 FROM #system.dual()");
        var diagnosticContext = new DiagnosticContext(sourceText);
        var input = new ValuesScanNode(
            "v",
            [],
            new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]));
        var expression = IrExpressionSourceSpans.Set(
            new BinaryOp(
                BinaryOpKind.Divide,
                new Literal(1, typeof(int)),
                new Literal(0, typeof(int)),
                typeof(int)),
            new TextSpan(7, 5));
        var initial = new ProjectNode(
            [new ProjectedField("Value", expression, 0)],
            input);

        _ = new LogicalOptimizer(diagnosticContext: diagnosticContext).Optimize(initial);

        var error = diagnosticContext.Errors.Single();
        Assert.AreEqual(DiagnosticCode.MQ3008_DivisionByZero, error.Code);
        Assert.AreEqual(1, error.Location.Line);
        Assert.AreEqual(8, error.Location.Column);
        Assert.AreEqual(13, error.EndLocation.Column);
    }

    [TestMethod]
    public void Optimize_WhenLogicalWherePredicateFoldsToFalse_ShouldReportContradictoryCondition()
    {
        var diagnosticContext = new DiagnosticContext(new SourceText("SELECT Value FROM v WHERE true AND false"));
        var input = new ValuesScanNode(
            "v",
            [],
            new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]));
        var predicate = IrExpressionSourceSpans.Set(
            new BinaryOp(
                BinaryOpKind.And,
                new Literal(true, typeof(bool)),
                new Literal(false, typeof(bool)),
                typeof(bool)),
            new TextSpan(26, 14));
        var initial = new FilterNode(predicate, input);

        _ = new LogicalOptimizer(diagnosticContext: diagnosticContext).Optimize(initial);

        var warning = diagnosticContext.Warnings.Single();
        Assert.AreEqual(DiagnosticCode.MQ5011_ContradictoryCondition, warning.Code);
        StringAssert.Contains(warning.Message, "WHERE");
    }

    private static void AssertTraceEntriesAreMeaningful(
        IReadOnlyList<OptimizationTraceEntry> entries)
    {
        Assert.IsTrue(entries.All(static entry =>
            entry.Stage is OptimizationStage.LogicalNormalization or OptimizationStage.LogicalOptimization));
        Assert.IsTrue(entries.All(static entry => !string.IsNullOrWhiteSpace(entry.PassName)));
        Assert.IsTrue(entries.All(static entry => !string.IsNullOrWhiteSpace(entry.Outcome)));
        Assert.IsTrue(entries.All(static entry => !string.IsNullOrWhiteSpace(entry.Reason)));
        Assert.IsTrue(entries.All(static entry =>
            string.Equals(entry.Outcome, entry.IsChanged ? "Changed" : "NoChange", StringComparison.Ordinal)));
    }
}
