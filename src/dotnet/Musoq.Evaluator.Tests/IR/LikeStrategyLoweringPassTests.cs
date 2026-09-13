using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class LikeStrategyLoweringPassTests
{
    [TestMethod]
    public void Optimize_WhenOuterPatternFeedsInnerRows_ShouldPrepareOnceBeforeInnerSetup()
    {
        var outer = Var("m1", typeof(object));
        var inner = Var("m2", typeof(object));
        var first = Match(Field("m2", "Value"), Field("m1", "Pattern"));
        var second = Match(Field("m2", "OtherValue"), Field("m1", "Pattern"));
        var plan = Plan(new ExecutionForEach(
            outer,
            new ExecutionVariableRead(Var("outerRows", typeof(object))),
            new ExecutionBlock(
            [
                new ExecutionEnumerableSource(
                    Var("innerRows", typeof(object)),
                    new ExecutionMemberRead(new ExecutionVariableRead(outer), "Children", typeof(object), false),
                    typeof(object)),
                new ExecutionForEach(
                    inner,
                    new ExecutionVariableRead(Var("innerRows", typeof(object))),
                    new ExecutionBlock(
                    [
                        new ExecutionLet(Var("first", typeof(bool)), first),
                        new ExecutionLet(Var("second", typeof(bool)), second)
                    ]))
            ])));

        var result = Optimize(plan);
        var optimizedOuter = Assert.IsInstanceOfType<ExecutionForEach>(result.Plan.Body.Nodes.Single());
        var preparation = Assert.IsInstanceOfType<ExecutionLet>(optimizedOuter.Body.Nodes[0]);

        Assert.IsInstanceOfType<ExecutionPrepareLikeMatcher>(preparation.Value);
        Assert.IsInstanceOfType<ExecutionEnumerableSource>(optimizedOuter.Body.Nodes[1]);
        Assert.IsInstanceOfType<ExecutionForEach>(optimizedOuter.Body.Nodes[2]);
        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareLikeMatcher>(result.Plan.Body));
        Assert.HasCount(2, ExecutionIrAnalysis.CollectExpressions<ExecutionPreparedLikeMatch>(result.Plan.Body));
        Assert.IsEmpty(ExecutionIrAnalysis.CollectExpressions<ExecutionDynamicLikeMatch>(result.Plan.Body));
        StringAssert.Contains(result.Reason, "loop-invariant prepared=2");
    }

    [TestMethod]
    public void Optimize_WhenPatternDependsOnInnerRow_ShouldRemainDynamic()
    {
        var outer = Var("m1", typeof(object));
        var inner = Var("m2", typeof(object));
        var plan = Plan(new ExecutionForEach(
            outer,
            new ExecutionVariableRead(Var("outerRows", typeof(object))),
            new ExecutionBlock(
            [
                new ExecutionForEach(
                    inner,
                    new ExecutionVariableRead(Var("innerRows", typeof(object))),
                    new ExecutionBlock(
                    [
                        new ExecutionLet(
                            Var("matched", typeof(bool)),
                            Match(Field("m1", "Value"), Field("m2", "Pattern")))
                    ]))
            ])));

        var result = Optimize(plan);

        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionDynamicLikeMatch>(result.Plan.Body));
        Assert.IsEmpty(ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareLikeMatcher>(result.Plan.Body));
    }

    [TestMethod]
    public void Optimize_WhenOuterLocalSuppliesPattern_ShouldPrepareAfterDependencyAndBeforeInnerLoop()
    {
        var outer = Var("m1", typeof(object));
        var inner = Var("m2", typeof(object));
        var pattern = Var("outerPattern", typeof(string));
        var plan = Plan(new ExecutionForEach(
            outer,
            new ExecutionVariableRead(Var("outerRows", typeof(object))),
            new ExecutionBlock(
            [
                new ExecutionLet(pattern, Field("m1", "Pattern")),
                new ExecutionForEach(
                    inner,
                    new ExecutionVariableRead(Var("innerRows", typeof(object))),
                    new ExecutionBlock(
                    [
                        new ExecutionLet(
                            Var("matched", typeof(bool)),
                            Match(Field("m2", "Value"), new ExecutionVariableRead(pattern)))
                    ]))
            ])));

        var result = Optimize(plan);
        var optimizedOuter = Assert.IsInstanceOfType<ExecutionForEach>(result.Plan.Body.Nodes.Single());

        Assert.AreSame(pattern, Assert.IsInstanceOfType<ExecutionLet>(optimizedOuter.Body.Nodes[0]).Variable);
        Assert.IsInstanceOfType<ExecutionPrepareLikeMatcher>(
            Assert.IsInstanceOfType<ExecutionLet>(optimizedOuter.Body.Nodes[1]).Value);
        Assert.IsInstanceOfType<ExecutionForEach>(optimizedOuter.Body.Nodes[2]);
    }

    [TestMethod]
    public void Optimize_WhenPatternIsScriptParameter_ShouldPrepareAtExecutionRoot()
    {
        var row = Var("row", typeof(object));
        var plan = Plan(new ExecutionForEach(
            row,
            new ExecutionVariableRead(Var("rows", typeof(object))),
            new ExecutionBlock(
            [
                new ExecutionLet(
                    Var("matched", typeof(bool)),
                    Match(
                        Field("row", "Value"),
                        new ExecutionScriptParameterRead("pattern", typeof(string))))
            ])));

        var result = Optimize(plan);

        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareLikeMatcher>(result.Plan.Body));
        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionPreparedLikeMatch>(result.Plan.Body));
        Assert.IsEmpty(ExecutionIrAnalysis.CollectExpressions<ExecutionDynamicLikeMatch>(result.Plan.Body));
    }

    [TestMethod]
    public void Optimize_WhenLikeIsInsideConditionalBody_ShouldNotHoistAcrossGuard()
    {
        var outer = Var("m1", typeof(object));
        var inner = Var("m2", typeof(object));
        var plan = Plan(new ExecutionForEach(
            outer,
            new ExecutionVariableRead(Var("outerRows", typeof(object))),
            new ExecutionBlock(
            [
                new ExecutionIf(
                    new ExecutionLiteral(true, typeof(bool)),
                    new ExecutionBlock(
                    [
                        new ExecutionForEach(
                            inner,
                            new ExecutionVariableRead(Var("innerRows", typeof(object))),
                            new ExecutionBlock(
                            [
                                new ExecutionLet(
                                    Var("matched", typeof(bool)),
                                    Match(Field("m2", "Value"), Field("m1", "Pattern")))
                            ]))
                    ]))
            ])));

        var result = Optimize(plan);

        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionDynamicLikeMatch>(result.Plan.Body));
        Assert.IsEmpty(ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareLikeMatcher>(result.Plan.Body));
    }

    [TestMethod]
    public void Optimize_WhenLikeIsInsideScopedBlock_ShouldNotHoistAcrossBoundary()
    {
        var outer = Var("m1", typeof(object));
        var inner = Var("m2", typeof(object));
        var plan = Plan(new ExecutionForEach(
            outer,
            new ExecutionVariableRead(Var("outerRows", typeof(object))),
            new ExecutionBlock(
            [
                new ExecutionScopedBlock(new ExecutionBlock(
                [
                    new ExecutionForEach(
                        inner,
                        new ExecutionVariableRead(Var("innerRows", typeof(object))),
                        new ExecutionBlock(
                        [
                            new ExecutionLet(
                                Var("matched", typeof(bool)),
                                Match(Field("m2", "Value"), Field("m1", "Pattern")))
                        ]))
                ]))
            ])));

        var result = Optimize(plan);

        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionDynamicLikeMatch>(result.Plan.Body));
        Assert.IsEmpty(ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareLikeMatcher>(result.Plan.Body));
    }

    [TestMethod]
    public void Optimize_WhenOuterPatternFieldIsVolatile_ShouldRemainDynamic()
    {
        var outer = Var("m1", typeof(object));
        var inner = Var("m2", typeof(object));
        var volatilePattern = Field("m1", "Pattern") with { Stability = ColumnStability.Volatile };
        var plan = Plan(new ExecutionForEach(
            outer,
            new ExecutionVariableRead(Var("outerRows", typeof(object))),
            new ExecutionBlock(
            [
                new ExecutionForEach(
                    inner,
                    new ExecutionVariableRead(Var("innerRows", typeof(object))),
                    new ExecutionBlock(
                    [
                        new ExecutionLet(
                            Var("matched", typeof(bool)),
                            Match(Field("m2", "Value"), volatilePattern))
                    ]))
            ])));

        var result = Optimize(plan);

        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionDynamicLikeMatch>(result.Plan.Body));
        Assert.IsEmpty(ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareLikeMatcher>(result.Plan.Body));
    }

    [TestMethod]
    public void Optimize_WhenStableMemberReadSuppliesOuterPattern_ShouldPrepareInOuterLoop()
    {
        var outer = Var("m1", typeof(object));
        var inner = Var("m2", typeof(object));
        var holder = new ExecutionFieldRead("m1", "Holder", typeof(PatternHolder));
        var pattern = new ExecutionMemberRead(holder, nameof(PatternHolder.Pattern), typeof(string), false);
        var plan = Plan(new ExecutionForEach(
            outer,
            new ExecutionVariableRead(Var("outerRows", typeof(object))),
            new ExecutionBlock(
            [
                new ExecutionForEach(
                    inner,
                    new ExecutionVariableRead(Var("innerRows", typeof(object))),
                    new ExecutionBlock(
                    [
                        new ExecutionLet(
                            Var("matched", typeof(bool)),
                            Match(Field("m2", "Value"), pattern))
                    ]))
            ])));

        var result = Optimize(plan);

        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareLikeMatcher>(result.Plan.Body));
        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionPreparedLikeMatch>(result.Plan.Body));
    }

    private static OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan) =>
        new LikeStrategyLoweringPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));

    private static ExecutionPlan Plan(ExecutionNode node) =>
        new("compiled", [], new ExecutionBlock([node]));

    private static ExecutionPatternMatch Match(ExecutionExpression input, ExecutionExpression pattern) =>
        new(input, pattern, PatternKind.Like, typeof(bool));

    private static ExecutionFieldRead Field(string alias, string name) =>
        new(alias, name, typeof(string));

    private static ExecutionVariable Var(string name, Type type) => new(name, type);

    private sealed class PatternHolder
    {
        public string Pattern { get; init; } = string.Empty;
    }
}
