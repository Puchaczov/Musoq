using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionLikeMatcherIrTests
{
    private static readonly ExecutionTypeRef MatcherType = ExecutionClrBindingFactory.FromClr(typeof(PreparedLikeMatcher));
    private static readonly ExecutionTypeRef CacheSlotType = ExecutionClrBindingFactory.FromClr(typeof(LikeMatcherCacheSlot));
    private static readonly ExecutionTypeRef BooleanType = ExecutionClrBindingFactory.FromClr(typeof(bool));

    [TestMethod]
    public void LikeMatcherOperations_ShouldParticipateInTraversalRewriteAndOperationRegistration()
    {
        var prepare = CreatePrepare(new ExecutionLiteral("a%", typeof(string)));
        var prepared = new ExecutionPreparedLikeMatch(
            new ExecutionLiteral("alpha", typeof(string)),
            prepare,
            BooleanType);
        var cacheSlot = new ExecutionLikeMatcherCacheSlot(CacheSlotType);
        var dynamic = new ExecutionDynamicLikeMatch(
            new ExecutionLiteral("alpha", typeof(string)),
            new ExecutionLiteral("a%", typeof(string)),
            cacheSlot,
            ExecutionStringMatchComparison.LikeIgnoreCase,
            BooleanType);

        CollectionAssert.AreEqual(
            new ExecutionExpression[] { prepare.Pattern },
            ExecutionIrAnalysis.GetChildExpressions(prepare).ToArray());
        Assert.HasCount(2, ExecutionIrAnalysis.GetChildExpressions(prepared).ToArray());
        Assert.HasCount(3, ExecutionIrAnalysis.GetChildExpressions(dynamic).ToArray());
        Assert.IsEmpty(ExecutionIrAnalysis.GetChildExpressions(cacheSlot).ToArray());

        var rewrittenPrepare = (ExecutionPrepareLikeMatcher)new LiteralReplacingRewriter().RewriteExpression(prepare);
        var rewrittenPrepared = (ExecutionPreparedLikeMatch)new LiteralReplacingRewriter().RewriteExpression(prepared);
        var rewrittenDynamic = (ExecutionDynamicLikeMatch)new LiteralReplacingRewriter().RewriteExpression(dynamic);
        Assert.AreEqual("changed", ((ExecutionLiteral)rewrittenPrepare.Pattern).Value.ToClrValue());
        Assert.AreEqual("changed", ((ExecutionLiteral)rewrittenPrepared.Input).Value.ToClrValue());
        Assert.AreEqual("changed", ((ExecutionLiteral)rewrittenDynamic.Pattern).Value.ToClrValue());

        Assert.AreEqual("expr.like.prepare-matcher", ExecutionOperationCatalog.Resolve(prepare).Value);
        Assert.AreEqual("expr.like.prepared-match", ExecutionOperationCatalog.Resolve(prepared).Value);
        Assert.AreEqual("expr.like.dynamic-match", ExecutionOperationCatalog.Resolve(dynamic).Value);
        Assert.AreEqual("expr.like.cache-slot", ExecutionOperationCatalog.Resolve(cacheSlot).Value);
    }

    [TestMethod]
    public void LikeMatcherOperations_ShouldParticipateInStabilityParallelCseAndFingerprints()
    {
        var prepare = CreatePrepare(new ExecutionLiteral("a%", typeof(string)));
        var prepared = new ExecutionPreparedLikeMatch(
            new ExecutionLiteral("alpha", typeof(string)),
            prepare,
            BooleanType);
        var cacheSlot = new ExecutionLikeMatcherCacheSlot(CacheSlotType);
        var dynamic = new ExecutionDynamicLikeMatch(
            new ExecutionLiteral("alpha", typeof(string)),
            new ExecutionLiteral("a%", typeof(string)),
            cacheSlot,
            ExecutionStringMatchComparison.LikeIgnoreCase,
            BooleanType);

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
        StringAssert.Contains(ExecutionExpressionFingerprint.ForHoist(prepare), "like-prepare:LikeIgnoreCase");
        StringAssert.Contains(ExecutionExpressionFingerprint.ForHoist(prepared), "like-prepared:");
        StringAssert.Contains(ExecutionExpressionFingerprint.ForHoist(dynamic), "like-dynamic:LikeIgnoreCase");
    }

    [TestMethod]
    public void LikeMatcherOperations_ShouldParticipateInCseSubstitutionAndPlanPrinting()
    {
        var input = new ExecutionStringMatch(
            new ExecutionLiteral("alpha", typeof(string)),
            "a%",
            "a",
            ExecutionStringMatchKind.Prefix,
            ExecutionStringMatchComparison.LikeIgnoreCase,
            BooleanType);
        var replacement = new ExecutionVariable("sharedMatch", typeof(bool));
        var cacheSlot = new ExecutionLikeMatcherCacheSlot(CacheSlotType);
        var dynamic = new ExecutionDynamicLikeMatch(
            input,
            new ExecutionLiteral("a%", typeof(string)),
            cacheSlot,
            ExecutionStringMatchComparison.LikeIgnoreCase,
            BooleanType);
        var replaced = (ExecutionDynamicLikeMatch)ExpressionCseSubstitution.Replace(
            dynamic,
            new System.Collections.Generic.Dictionary<string, ExecutionVariable>
            {
                [ExecutionExpressionFingerprint.ForHoist(input)] = replacement
            });
        var matcherVariable = new ExecutionVariable("matcher", MatcherType);
        var cacheVariable = new ExecutionVariable("likeCache", CacheSlotType);
        var plan = new ExecutionPlan(
            "Q_LikeMatcherIr",
            [],
            new ExecutionBlock([
                new ExecutionLet(cacheVariable, cacheSlot),
                new ExecutionLet(matcherVariable, CreatePrepare(new ExecutionLiteral("a%", typeof(string)))),
                new ExecutionLet(
                    new ExecutionVariable("preparedResult", typeof(bool)),
                    new ExecutionPreparedLikeMatch(
                        new ExecutionLiteral("alpha", typeof(string)),
                        new ExecutionVariableRead(matcherVariable),
                        BooleanType)),
                new ExecutionLet(
                    new ExecutionVariable("dynamicResult", typeof(bool)),
                    new ExecutionDynamicLikeMatch(
                        new ExecutionLiteral("alpha", typeof(string)),
                        new ExecutionLiteral("a%", typeof(string)),
                        new ExecutionVariableRead(cacheVariable),
                        ExecutionStringMatchComparison.LikeIgnoreCase,
                        BooleanType))
            ]));
        var text = ExecutionPlanPrinter.Print(plan);

        Assert.AreSame(replacement, ((ExecutionVariableRead)replaced.Input).Variable);
        StringAssert.Contains(text, "LIKE_MATCHER_CACHE_SLOT(capacity=2, scope=serial)");
        StringAssert.Contains(text, "PREPARE_LIKE('a%', comparison=LikeIgnoreCase)");
        StringAssert.Contains(text, "PREPARED_LIKE('alpha', matcher)");
        StringAssert.Contains(text, "DYNAMIC_LIKE('alpha', 'a%', cache=likeCache, comparison=LikeIgnoreCase)");
    }

    private static ExecutionPrepareLikeMatcher CreatePrepare(ExecutionExpression pattern) =>
        new(pattern, ExecutionStringMatchComparison.LikeIgnoreCase, MatcherType);

    private sealed class LiteralReplacingRewriter : ExecutionIrRewriter
    {
        protected override ExecutionExpression RewriteLiteral(ExecutionLiteral expression) =>
            new ExecutionLiteral("changed", typeof(string));
    }
}
