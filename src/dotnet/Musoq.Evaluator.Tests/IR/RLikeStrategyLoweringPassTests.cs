using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class RLikeStrategyLoweringPassTests
{
    [TestMethod]
    public void Optimize_WhenConstantContainsRegexSyntax_ShouldPrepareAtExecutionRoot()
    {
        var plan = Plan(new ExecutionLet(
            Var("matched", typeof(bool)),
            Match(new ExecutionLiteral("alpha", typeof(string)), new ExecutionLiteral("^a", typeof(string)))));

        var result = Optimize(plan);

        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareRLikeMatcher>(result.Plan.Body));
        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionPreparedRLikeMatch>(result.Plan.Body));
        Assert.IsEmpty(ExecutionIrAnalysis.CollectExpressions<ExecutionPatternMatch>(result.Plan.Body));
        StringAssert.Contains(result.Reason, "RLIKE(prepared=1, dynamic=0");
        StringAssert.Contains(result.Reason, "fallbacks=[RegexMetacharacter=1]");
        StringAssert.Contains(
            ExecutionPlanPrinter.Print(result.Plan),
            "strategy=regex, anchors=none, literal-span=none, fallback=RegexMetacharacter");
    }

    [TestMethod]
    public void Optimize_WhenConstantPatternsAreProvablyLiteral_ShouldLowerToOrdinalStringMatches()
    {
        var patterns = new[] { "alpha", @"\Aalpha", @"alpha\z", @"\Aalpha\z" };
        var body = patterns
            .Select((pattern, index) => (ExecutionNode)new ExecutionLet(
                Var($"matched{index}", typeof(bool)),
                Match(new ExecutionLiteral("head-alpha-tail", typeof(string)), new ExecutionLiteral(pattern, typeof(string)))))
            .ToArray();

        var result = Optimize(new ExecutionPlan("compiled", [], new ExecutionBlock(body)));
        var matches = ExecutionIrAnalysis.CollectExpressions<ExecutionStringMatch>(result.Plan.Body).ToArray();

        Assert.HasCount(4, matches);
        CollectionAssert.AreEquivalent(
            Enum.GetValues<ExecutionStringMatchKind>(),
            matches.Select(static match => match.Kind).ToArray());
        Assert.IsTrue(matches.All(static match => match.Comparison == ExecutionStringMatchComparison.Ordinal));
        Assert.IsTrue(matches.All(static match => match.Needle == "alpha"));
        Assert.IsEmpty(ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareRLikeMatcher>(result.Plan.Body));
        Assert.IsEmpty(ExecutionIrAnalysis.CollectExpressions<ExecutionPreparedRLikeMatch>(result.Plan.Body));
        StringAssert.Contains(result.Reason, "direct=4");
        StringAssert.Contains(result.Reason, "fallbacks=[none]");
        var planText = ExecutionPlanPrinter.Print(result.Plan);
        StringAssert.Contains(planText, "anchors=None, literal-span=0:5, fallback=none");
        StringAssert.Contains(planText, "anchors=Start, literal-span=2:5, fallback=none");
        StringAssert.Contains(planText, "anchors=End, literal-span=0:5, fallback=none");
        StringAssert.Contains(planText, "anchors=StartAndEnd, literal-span=2:5, fallback=none");
    }

    [TestMethod]
    public void Optimize_WhenConstantUsesUnsupportedEscape_ShouldReportStableFallbackReason()
    {
        var result = Optimize(Plan(new ExecutionLet(
            Var("matched", typeof(bool)),
            Match(new ExecutionLiteral("alpha1", typeof(string)), new ExecutionLiteral(@"alpha\d", typeof(string))))));

        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionPreparedRLikeMatch>(result.Plan.Body));
        StringAssert.Contains(result.Reason, "fallbacks=[UnsupportedEscape=1]");
    }

    [TestMethod]
    public void Optimize_WhenPatternIsDynamicInSerialRegion_ShouldUseOneTwoEntryCacheSlot()
    {
        var row = Var("row", typeof(object));
        var plan = Plan(new ExecutionForEach(
            row,
            new ExecutionVariableRead(Var("rows", typeof(object))),
            new ExecutionBlock([
                new ExecutionLet(Var("first", typeof(bool)), Match(Field("row", "First"), Field("row", "Pattern"))),
                new ExecutionLet(Var("second", typeof(bool)), Match(Field("row", "Second"), Field("row", "OtherPattern")))
            ])));

        var result = Optimize(plan);
        var slots = ExecutionIrAnalysis.CollectExpressions<ExecutionRLikeMatcherCacheSlot>(result.Plan.Body).ToArray();

        Assert.HasCount(1, slots);
        Assert.IsFalse(slots[0].WorkerLocal);
        Assert.HasCount(2, ExecutionIrAnalysis.CollectExpressions<ExecutionDynamicRLikeMatch>(result.Plan.Body));
        StringAssert.Contains(result.Reason, "dynamic=2");
        StringAssert.Contains(result.Reason, "serial-cache=1");
    }

    [TestMethod]
    public void Optimize_WhenOuterPatternFeedsInnerRows_ShouldPrepareOnceBeforeInnerSetup()
    {
        var outer = Var("m1", typeof(object));
        var inner = Var("m2", typeof(object));
        var plan = Plan(new ExecutionForEach(
            outer,
            new ExecutionVariableRead(Var("outerRows", typeof(object))),
            new ExecutionBlock([
                new ExecutionEnumerableSource(
                    Var("innerRows", typeof(object)),
                    new ExecutionMemberRead(new ExecutionVariableRead(outer), "Children", typeof(object), false),
                    typeof(object)),
                new ExecutionForEach(
                    inner,
                    new ExecutionVariableRead(Var("innerRows", typeof(object))),
                    new ExecutionBlock([
                        new ExecutionLet(Var("first", typeof(bool)), Match(Field("m2", "Value"), Field("m1", "Pattern"))),
                        new ExecutionLet(Var("second", typeof(bool)), Match(Field("m2", "Other"), Field("m1", "Pattern")))
                    ]))
            ])));

        var result = Optimize(plan);
        var optimizedOuter = Assert.IsInstanceOfType<ExecutionForEach>(result.Plan.Body.Nodes.Single());

        Assert.IsInstanceOfType<ExecutionPrepareRLikeMatcher>(
            Assert.IsInstanceOfType<ExecutionLet>(optimizedOuter.Body.Nodes[0]).Value);
        Assert.IsInstanceOfType<ExecutionEnumerableSource>(optimizedOuter.Body.Nodes[1]);
        Assert.IsInstanceOfType<ExecutionForEach>(optimizedOuter.Body.Nodes[2]);
        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareRLikeMatcher>(result.Plan.Body));
        Assert.HasCount(2, ExecutionIrAnalysis.CollectExpressions<ExecutionPreparedRLikeMatch>(result.Plan.Body));
        Assert.IsEmpty(ExecutionIrAnalysis.CollectExpressions<ExecutionDynamicRLikeMatch>(result.Plan.Body));
        StringAssert.Contains(result.Reason, "RLIKE(prepared=2, dynamic=0, loop-invariant=2");
    }

    [TestMethod]
    public void Optimize_WhenPatternDependsOnInnerRow_ShouldRemainDynamic()
    {
        var outer = Var("m1", typeof(object));
        var inner = Var("m2", typeof(object));
        var plan = Plan(new ExecutionForEach(
            outer,
            new ExecutionVariableRead(Var("outerRows", typeof(object))),
            new ExecutionBlock([
                new ExecutionForEach(
                    inner,
                    new ExecutionVariableRead(Var("innerRows", typeof(object))),
                    new ExecutionBlock([
                        new ExecutionLet(
                            Var("matched", typeof(bool)),
                            Match(Field("m1", "Value"), Field("m2", "Pattern")))
                    ]))
            ])));

        var result = Optimize(plan);

        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionDynamicRLikeMatch>(result.Plan.Body));
        Assert.IsEmpty(ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareRLikeMatcher>(result.Plan.Body));
    }

    [TestMethod]
    public void Optimize_WhenRLikeIsInsideConditionalBody_ShouldNotHoistAcrossGuard()
    {
        var outer = Var("m1", typeof(object));
        var inner = Var("m2", typeof(object));
        var plan = Plan(new ExecutionForEach(
            outer,
            new ExecutionVariableRead(Var("outerRows", typeof(object))),
            new ExecutionBlock([
                new ExecutionIf(
                    new ExecutionLiteral(true, typeof(bool)),
                    new ExecutionBlock([
                        new ExecutionForEach(
                            inner,
                            new ExecutionVariableRead(Var("innerRows", typeof(object))),
                            new ExecutionBlock([
                                new ExecutionLet(
                                    Var("matched", typeof(bool)),
                                    Match(Field("m2", "Value"), Field("m1", "Pattern")))
                            ]))
                    ]))
            ])));

        var result = Optimize(plan);

        Assert.HasCount(1, ExecutionIrAnalysis.CollectExpressions<ExecutionDynamicRLikeMatch>(result.Plan.Body));
        Assert.IsEmpty(ExecutionIrAnalysis.CollectExpressions<ExecutionPrepareRLikeMatcher>(result.Plan.Body));
    }

    private static OptimizationResult<ExecutionPlan> Optimize(ExecutionPlan plan) =>
        new LikeStrategyLoweringPass().Optimize(
            plan,
            new OptimizationContext(OptimizationStage.ExecutionIrOptimization));

    private static ExecutionPlan Plan(ExecutionNode node) =>
        new("compiled", [], new ExecutionBlock([node]));

    private static ExecutionPatternMatch Match(ExecutionExpression input, ExecutionExpression pattern) =>
        new(input, pattern, PatternKind.RLike, typeof(bool));

    private static ExecutionFieldRead Field(string alias, string name) =>
        new(alias, name, typeof(string));

    private static ExecutionVariable Var(string name, Type type) => new(name, type);
}
