using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Optimization;
using LogicalConstantFoldingPass = Musoq.Evaluator.IR.Optimization.Logical.LogicalConstantFoldingPass;

namespace Musoq.Evaluator.Tests.Architecture;

[TestClass]
public sealed class OptimizerContextOptionGuardrailTests
{
    [TestMethod]
    public void DefaultConstructor_ShouldExposeDefaultOptionsAndEmptyState()
    {
        var context = new OptimizationContext(OptimizationStage.LogicalOptimization);

        Assert.AreSame(OptimizationOptions.Default, context.Options);
        Assert.AreSame(OptimizationContextState.Empty, context.State);
    }

    [TestMethod]
    public void TypedConstructor_ShouldExposeProvidedOptionsAndState()
    {
        var options = new OptimizationOptions { ConstantFoldingEnabled = false };
        var state = new OptimizationContextState();

        var context = new OptimizationContext(
            OptimizationStage.LogicalOptimization,
            trace: null,
            options,
            state);

        Assert.AreSame(options, context.Options);
        Assert.AreSame(state, context.State);
    }

    [TestMethod]
    public void OptimizationContext_ShouldNotExposeRawPropertiesBag()
    {
        Assert.IsNull(
            typeof(OptimizationContext).GetProperty("Properties"),
            "Optimizer state must flow through OptimizationOptions and OptimizationContextState, not a raw property bag.");
    }

    [TestMethod]
    public void DefaultOptions_ShouldMatchLegacyMissingKeySemantics()
    {
        var options = OptimizationOptions.Default;

        Assert.IsTrue(options.ConstantFoldingEnabled);
        Assert.IsTrue(options.FieldReadDiscoveryEnabled);
        Assert.IsTrue(options.ExpressionCseEnabled);
        Assert.IsFalse(options.CrossNodeExpressionCseEnabled);
    }

    [TestMethod]
    public void LogicalConstantFoldingPass_WhenConstantFoldingDisabledViaOptions_ShouldNotFold()
    {
        var plan = BuildFoldablePlan();
        var context = new OptimizationContext(
            OptimizationStage.LogicalOptimization,
            trace: null,
            new OptimizationOptions { ConstantFoldingEnabled = false },
            OptimizationContextState.Empty);

        var result = new LogicalConstantFoldingPass().Optimize(plan, context);

        Assert.IsFalse(result.IsChanged);
        Assert.AreEqual("Logical constant folding is disabled by compilation options.", result.Reason);
    }

    [TestMethod]
    public void LogicalConstantFoldingPass_WhenConstantFoldingEnabledViaOptions_ShouldFold()
    {
        var plan = BuildFoldablePlan();
        var context = new OptimizationContext(
            OptimizationStage.LogicalOptimization,
            trace: null,
            new OptimizationOptions { ConstantFoldingEnabled = true },
            OptimizationContextState.Empty);

        var result = new LogicalConstantFoldingPass().Optimize(plan, context);

        Assert.IsTrue(result.IsChanged);
        var optimized = (ProjectNode)result.Plan;
        var literal = (Literal)optimized.Fields[0].Expression;
        Assert.AreEqual(3, literal.Value);
    }

    private static ProjectNode BuildFoldablePlan()
    {
        var input = new ValuesScanNode(
            "v",
            [],
            new OutputSchema([new ColumnSchema("Value", typeof(int), 0)]));

        return new ProjectNode(
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
    }
}
