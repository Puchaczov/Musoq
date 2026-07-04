using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionExpressionCseFactsTests
{
    [TestMethod]
    public void Analyze_WhenRepeatedDeterministicMethodIsHoisted_ShouldReportNoSkippedGroups()
    {
        var method = new ExecutionMethodCall(
            GetType().GetMethod(nameof(Identity), BindingFlags.Public | BindingFlags.Static)!,
            [new ExecutionLiteral(1, typeof(int))],
            null,
            typeof(int));
        var plan = CreatePlan(CreateAppendRow(method, method));

        var result = Optimize(plan);
        var diagnostics = ExpressionCseSkipDiagnostics.Analyze(plan);

        Assert.IsTrue(result.IsChanged);
        Assert.IsFalse(diagnostics.HasSkippedOpportunities);
    }

    [TestMethod]
    public void Analyze_WhenShortCircuitExpressionIsPartlyHoisted_ShouldReportNoSkippedGroups()
    {
        var comparison = new ExecutionBinary(
            BinaryOpKind.GreaterThan,
            new ExecutionLiteral(2, typeof(int)),
            new ExecutionLiteral(1, typeof(int)),
            typeof(bool));
        var expression = new ExecutionBinary(
            BinaryOpKind.And,
            comparison,
            comparison,
            typeof(bool));
        var plan = CreatePlan(CreateAppendRow(expression, expression));

        var result = Optimize(plan);
        var diagnostics = ExpressionCseSkipDiagnostics.Analyze(plan);

        Assert.IsTrue(result.IsChanged);
        Assert.IsFalse(diagnostics.HasSkippedOpportunities);
    }

    [TestMethod]
    public void Analyze_WhenWindowHelperExpressionsAreIndependent_ShouldMatchHoistingPass()
    {
        var expression = CreateRepeatedLiteralExpression();
        var row = new ExecutionVariable("row", typeof(object));
        var window = new ExecutionComputeOffsetWindow(
            new ExecutionVariable("windowRows", typeof(object)),
            row,
            ExecutionRowAccessMode.Direct,
            expression,
            [new ExecutionWindowOrderKey(expression, false)],
            expression,
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(0, typeof(int)),
            ExecutionOffsetWindowFunction.Lag,
            new ExecutionVariable("windowResults", typeof(object)));
        var plan = CreatePlan(window);

        var result = Optimize(plan);
        var diagnostics = ExpressionCseSkipDiagnostics.Analyze(plan);

        Assert.IsTrue(result.IsChanged);
        Assert.IsFalse(diagnostics.HasSkippedOpportunities);
    }

    [TestMethod]
    public void Analyze_WhenGeneratedHelperBodyHasRepeatedUnsupportedExpressions_ShouldReportGeneratedHelperGroup()
    {
        var expression = CreateRepeatedLiteralExpression();
        var helperBody = new ExecutionBlock(
        [
            new ExecutionLet(new ExecutionVariable("first", typeof(int)), expression),
            new ExecutionLet(new ExecutionVariable("second", typeof(int)), expression)
        ]);
        var plan = CreatePlan(new ExecutionForEachIndexed(
            new ExecutionVariable("item", typeof(object)),
            new ExecutionVariable("index", typeof(int)),
            new ExecutionVariable("rows", typeof(object)),
            ExecutionRowAccessMode.Direct,
            helperBody));

        var result = Optimize(plan);
        var diagnostics = ExpressionCseSkipDiagnostics.Analyze(plan);

        Assert.IsFalse(result.IsChanged);
        Assert.AreEqual(1, diagnostics.GeneratedHelperBodyGroups);
        Assert.IsTrue(diagnostics.HasSkippedOpportunities);
    }

    public static int Identity(int value)
    {
        return value;
    }

    private static OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan)
    {
        return new ExpressionCseHoistingPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));
    }

    private static ExecutionPlan CreatePlan(params ExecutionNode[] nodes)
    {
        return new ExecutionPlan("compiled", [], new ExecutionBlock(nodes));
    }

    private static ExecutionAppendRow CreateAppendRow(
        ExecutionExpression first,
        ExecutionExpression second)
    {
        return new ExecutionAppendRow(
            new ExecutionVariable("result", typeof(object)),
            CreateRowShape(),
            [
                new ExecutionRowValue("First", first),
                new ExecutionRowValue("Second", second)
            ]);
    }

    private static GeneratedRowShape CreateRowShape()
    {
        return new GeneratedRowShape(
            "ResultRow0",
            [
                new FieldBinding("First", "First", 0, typeof(object), FieldNullability.Unknown, new GeneratedFieldAccess("First")),
                new FieldBinding("Second", "Second", 1, typeof(object), FieldNullability.Unknown, new GeneratedFieldAccess("Second"))
            ]);
    }

    private static ExecutionExpression CreateRepeatedLiteralExpression()
    {
        return new ExecutionBinary(
            BinaryOpKind.Add,
            new ExecutionLiteral(1, typeof(int)),
            new ExecutionLiteral(2, typeof(int)),
            typeof(int));
    }
}
