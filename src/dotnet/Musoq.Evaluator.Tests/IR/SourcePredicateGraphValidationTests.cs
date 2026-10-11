using System;
using System.Threading;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.IR.Planning.SourcePlanning;
using Musoq.Evaluator.IR.SourcePlanning;
using Musoq.Parser;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.IR;

[TestClass]
public sealed class SourcePredicateGraphValidationTests
{
    [TestMethod]
    public void ProviderReplyBeyondDepthBudget_ShouldProduceContractDiagnostic()
    {
        var predicate = Nested(257);
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with { Predicate = predicate };
        Assert.Throws<SourcePlanContractException>(() =>
            SourcePredicatePlanContractValidator.Validate(request, SourcePlanResult.RejectAll(request), TextSpan.Empty));
    }

    [TestMethod]
    public void SharedNodesAndDuplicateConjuncts_ShouldRemainValid()
    {
        var leaf = new SourcePredicateComparison(SourcePredicateComparisonOperator.Equal,
            new SourcePredicateLiteral(1), new SourcePredicateLiteral(1));
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            Predicate = new SourcePredicateLogical(SourcePredicateLogicalOperator.And, leaf, leaf)
        };
        SourcePredicatePlanContractValidator.Validate(request, SourcePlanResult.RejectAll(request), TextSpan.Empty);
    }

    [TestMethod]
    public void CancelledGraphValidation_ShouldObserveCancellation()
    {
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with
        {
            Predicate = Nested(64),
            CancellationToken = new CancellationToken(true)
        };
        Assert.Throws<OperationCanceledException>(() =>
            SourcePredicatePlanContractValidator.Validate(request, SourcePlanResult.RejectAll(request), TextSpan.Empty));
    }

    private static SourcePredicateExpression Nested(int depth)
    {
        SourcePredicateExpression result = new SourcePredicateLiteral(true);
        for (var level = 1; level < depth; level++)
            result = new SourcePredicateLogical(SourcePredicateLogicalOperator.And, result, new SourcePredicateLiteral(true));
        return result;
    }

    [TestMethod]
    [DataRow(256, true)]
    [DataRow(257, false)]
    [DataRow(16000, false)]
    public void DepthLimit_ShouldBoundTraversalWithoutRecursion(int depth, bool expected)
    {
        Assert.AreEqual(expected, SourcePredicateGraphValidator.IsValid(Nested(depth), CancellationToken.None));
    }

    [TestMethod]
    [DataRow(99998, true)]
    [DataRow(99999, false)]
    public void ExpandedOccurrenceLimit_ShouldCountSharedLeaves(int members, bool expected)
    {
        var leaf = new SourcePredicateLiteral(1);
        var predicate = new SourcePredicateIn(leaf, Enumerable.Repeat<SourcePredicateExpression>(leaf, members).ToArray());
        Assert.AreEqual(expected, SourcePredicateGraphValidator.IsValid(predicate, CancellationToken.None));
    }

    [TestMethod]
    public void CyclicOrMalformedReplies_ShouldProduceContractDiagnostic()
    {
        var cycle = new SourcePredicateLogical(SourcePredicateLogicalOperator.And,
            new SourcePredicateLiteral(true), new SourcePredicateLiteral(true));
        typeof(SourcePredicateLogical).GetProperty(nameof(SourcePredicateLogical.Left))!.SetValue(cycle, cycle);
        foreach (var predicate in new SourcePredicateExpression[] { cycle, new SourcePredicateLiteral(2),
                     new SourcePredicateLogical(SourcePredicateLogicalOperator.And, null!, new SourcePredicateLiteral(true)) })
        {
            var request = SourcePlanRequest.Empty(SourceIdentity.Empty);
            var result = SourcePlanResult.RejectAll(request) with { ResidualPredicate = predicate };
            Assert.Throws<SourcePlanContractException>(() =>
                SourcePredicatePlanContractValidator.Validate(request, result, TextSpan.Empty));
        }
    }

    [TestMethod]
    public void OversizedEngineCandidate_ShouldNotReachCapabilityNegotiation()
    {
        var request = SourcePlanRequest.Empty(SourceIdentity.Empty) with { Predicate = Nested(16000) };
        var negotiation = SourcePredicateCapabilityNegotiator.Negotiate(request, new SourcePredicateCapabilities());
        Assert.IsNull(negotiation.ProviderRequest.Predicate);
    }
}
