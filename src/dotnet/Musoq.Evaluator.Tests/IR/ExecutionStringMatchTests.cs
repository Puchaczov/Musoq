using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.IR.Execution;
using Musoq.Targets.Execution.Analysis;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class ExecutionStringMatchTests
{
    [TestMethod]
    public void StringMatch_ShouldParticipateInTraversalRewriteFingerprintAndPrinting()
    {
        var input = new ExecutionLiteral("Google", typeof(string));
        var match = CreateMatch(input);
        var plan = new ExecutionPlan(
            "Q_StringMatch",
            [],
            new ExecutionBlock([
                new ExecutionLet(new ExecutionVariable("matched", typeof(bool)), match)
            ]));

        var children = ExecutionIrAnalysis.GetChildExpressions(match).ToArray();
        var rewritten = new LiteralReplacingRewriter().RewriteExpression(match);

        Assert.HasCount(1, children);
        Assert.AreSame(input, children[0]);
        Assert.IsInstanceOfType<ExecutionStringMatch>(rewritten);
        Assert.AreEqual("changed", ((ExecutionLiteral)((ExecutionStringMatch)rewritten).Input).Value.ToClrValue());
        Assert.AreEqual("expr.string-match", ExecutionOperationCatalog.Resolve(match).Value);
        StringAssert.Contains(ExecutionExpressionFingerprint.ForHoist(match), "string-match:Prefix:LikeIgnoreCase");
        StringAssert.Contains(ExecutionPlanPrinter.Print(plan), "STRING_MATCH");
        Assert.AreEqual(8, plan.ExecutionIrVersion);
    }

    [TestMethod]
    public void StringMatch_ShouldBeStableParallelEligibleAndCseEligible()
    {
        var match = CreateMatch(new ExecutionLiteral("Google", typeof(string)));

        Assert.IsTrue(Musoq.Evaluator.IR.Analysis.ExpressionStabilityAnalyzer.IsStable(match));
        Assert.IsTrue(ParallelExecutionEligibilityRules.CanUseFilterProjectExpression(match).IsEligible);
        Assert.IsTrue(Musoq.Evaluator.IR.Optimization.Execution.ExecutionExpressionCseFacts.IsWorthHoistingExpression(match));
    }

    private static ExecutionStringMatch CreateMatch(ExecutionExpression input) =>
        new(
            input,
            "Google%",
            "Google",
            ExecutionStringMatchKind.Prefix,
            ExecutionStringMatchComparison.LikeIgnoreCase,
            ExecutionClrBindingFactory.FromClr(typeof(bool)));

    private sealed class LiteralReplacingRewriter : ExecutionIrRewriter
    {
        protected override ExecutionExpression RewriteLiteral(ExecutionLiteral expression) =>
            new ExecutionLiteral("changed", typeof(string));
    }
}
