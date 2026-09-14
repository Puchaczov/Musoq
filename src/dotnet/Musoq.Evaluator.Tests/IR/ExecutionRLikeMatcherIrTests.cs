using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionRLikeMatcherIrTests
{
    private static readonly ExecutionTypeRef MatcherType = ExecutionClrBindingFactory.FromClr(typeof(PreparedRLikeMatcher));
    private static readonly ExecutionTypeRef CacheSlotType = ExecutionClrBindingFactory.FromClr(typeof(RLikeMatcherCacheSlot));
    private static readonly ExecutionTypeRef BooleanType = ExecutionClrBindingFactory.FromClr(typeof(bool));

    [TestMethod]
    public void RLikeMatcherOperations_ShouldParticipateInTraversalRewriteAndOperationRegistration()
    {
        var prepare = CreatePrepare(new ExecutionLiteral("^a", typeof(string)));
        var prepared = new ExecutionPreparedRLikeMatch(
            new ExecutionLiteral("alpha", typeof(string)),
            prepare,
            BooleanType);
        var cacheSlot = new ExecutionRLikeMatcherCacheSlot(CacheSlotType);
        var dynamic = new ExecutionDynamicRLikeMatch(
            new ExecutionLiteral("alpha", typeof(string)),
            new ExecutionLiteral("^a", typeof(string)),
            cacheSlot,
            BooleanType);

        CollectionAssert.AreEqual(
            new ExecutionExpression[] { prepare.Pattern },
            ExecutionIrAnalysis.GetChildExpressions(prepare).ToArray());
        Assert.HasCount(2, ExecutionIrAnalysis.GetChildExpressions(prepared).ToArray());
        Assert.HasCount(3, ExecutionIrAnalysis.GetChildExpressions(dynamic).ToArray());
        Assert.IsEmpty(ExecutionIrAnalysis.GetChildExpressions(cacheSlot).ToArray());

        var rewrittenPrepare = (ExecutionPrepareRLikeMatcher)new LiteralReplacingRewriter().RewriteExpression(prepare);
        var rewrittenPrepared = (ExecutionPreparedRLikeMatch)new LiteralReplacingRewriter().RewriteExpression(prepared);
        var rewrittenDynamic = (ExecutionDynamicRLikeMatch)new LiteralReplacingRewriter().RewriteExpression(dynamic);
        Assert.AreEqual("changed", ((ExecutionLiteral)rewrittenPrepare.Pattern).Value.ToClrValue());
        Assert.AreEqual("changed", ((ExecutionLiteral)rewrittenPrepared.Input).Value.ToClrValue());
        Assert.AreEqual("changed", ((ExecutionLiteral)rewrittenDynamic.Pattern).Value.ToClrValue());

        Assert.AreEqual("expr.rlike.prepare-matcher", ExecutionOperationCatalog.Resolve(prepare).Value);
        Assert.AreEqual("expr.rlike.prepared-match", ExecutionOperationCatalog.Resolve(prepared).Value);
        Assert.AreEqual("expr.rlike.dynamic-match", ExecutionOperationCatalog.Resolve(dynamic).Value);
        Assert.AreEqual("expr.rlike.cache-slot", ExecutionOperationCatalog.Resolve(cacheSlot).Value);
    }

    [TestMethod]
    public void RLikeMatcherOperations_ShouldParticipateInStabilityParallelCseFingerprintsAndSubstitution()
    {
        var prepare = CreatePrepare(new ExecutionLiteral("^a", typeof(string)));
        var prepared = new ExecutionPreparedRLikeMatch(
            new ExecutionLiteral("alpha", typeof(string)),
            prepare,
            BooleanType);
        var cacheSlot = new ExecutionRLikeMatcherCacheSlot(CacheSlotType);
        var dynamic = new ExecutionDynamicRLikeMatch(
            prepared,
            new ExecutionLiteral("^a", typeof(string)),
            cacheSlot,
            BooleanType);
        var replacement = new ExecutionVariable("sharedMatch", BooleanType);
        var replaced = (ExecutionDynamicRLikeMatch)ExpressionCseSubstitution.Replace(
            dynamic,
            new System.Collections.Generic.Dictionary<string, ExecutionVariable>
            {
                [ExecutionExpressionFingerprint.ForHoist(prepared)] = replacement
            });

        Assert.IsTrue(ExpressionStabilityAnalyzer.IsStable(prepare));
        Assert.IsTrue(ExpressionStabilityAnalyzer.IsStable(prepared));
        Assert.IsTrue(ExpressionStabilityAnalyzer.IsStable(dynamic));
        Assert.IsFalse(ExpressionStabilityAnalyzer.IsStable(cacheSlot));
        Assert.IsTrue(ParallelExecutionEligibilityRules.CanUseFilterProjectExpression(prepared).IsEligible);
        Assert.IsTrue(ParallelExecutionEligibilityRules.CanUseFilterProjectExpression(dynamic).IsEligible);
        Assert.IsTrue(ExecutionExpressionCseFacts.IsWorthHoistingExpression(prepare));
        Assert.IsTrue(ExecutionExpressionCseFacts.IsWorthHoistingExpression(prepared));
        Assert.IsTrue(ExecutionExpressionCseFacts.IsWorthHoistingExpression(dynamic));
        Assert.IsFalse(ExecutionExpressionCseFacts.IsWorthHoistingExpression(cacheSlot));
        StringAssert.Contains(ExecutionExpressionFingerprint.ForHoist(prepare), "rlike-prepare:");
        StringAssert.Contains(ExecutionExpressionFingerprint.ForHoist(prepared), "rlike-prepared:");
        StringAssert.Contains(ExecutionExpressionFingerprint.ForHoist(dynamic), "rlike-dynamic:");
        Assert.AreSame(replacement, ((ExecutionVariableRead)replaced.Input).Variable);
    }

    [TestMethod]
    public void RLikeMatcherOperations_ShouldPrintSemanticPlanText()
    {
        var matcherVariable = new ExecutionVariable("matcher", MatcherType);
        var cacheVariable = new ExecutionVariable("rlikeCache", CacheSlotType);
        var plan = new ExecutionPlan(
            "Q_RLikeMatcherIr",
            [],
            new ExecutionBlock([
                new ExecutionLet(cacheVariable, new ExecutionRLikeMatcherCacheSlot(CacheSlotType)),
                new ExecutionLet(matcherVariable, CreatePrepare(new ExecutionLiteral("^a", typeof(string)))),
                new ExecutionLet(
                    new ExecutionVariable("preparedResult", typeof(bool)),
                    new ExecutionPreparedRLikeMatch(
                        new ExecutionLiteral("alpha", typeof(string)),
                        new ExecutionVariableRead(matcherVariable),
                        BooleanType)),
                new ExecutionLet(
                    new ExecutionVariable("dynamicResult", typeof(bool)),
                    new ExecutionDynamicRLikeMatch(
                        new ExecutionLiteral("alpha", typeof(string)),
                        new ExecutionLiteral("^a", typeof(string)),
                        new ExecutionVariableRead(cacheVariable),
                        BooleanType))
            ]));
        var text = ExecutionPlanPrinter.Print(plan);

        StringAssert.Contains(text, "RLIKE_MATCHER_CACHE_SLOT(capacity=2, scope=serial)");
        StringAssert.Contains(
            text,
            "PREPARE_RLIKE('^a', strategy=regex, anchors=none, literal-span=none, fallback=RegexMetacharacter)");
        StringAssert.Contains(text, "PREPARED_RLIKE('alpha', matcher)");
        StringAssert.Contains(
            text,
            "DYNAMIC_RLIKE('alpha', '^a', cache=rlikeCache, strategy=runtime-classified)");
    }

    private static ExecutionPrepareRLikeMatcher CreatePrepare(ExecutionExpression pattern) =>
        new(pattern, MatcherType);

    private sealed class LiteralReplacingRewriter : ExecutionIrRewriter
    {
        protected override ExecutionExpression RewriteLiteral(ExecutionLiteral expression) =>
            new ExecutionLiteral("changed", typeof(string));
    }
}
